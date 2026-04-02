 -- Remove nested_parts referencing the order items being deleted
  DELETE FROM nested_parts
  WHERE "OrderItemId" IN (
      SELECT oi."Id" FROM order_items oi
      JOIN nest_orders no ON no."Id" = oi."NestOrderId"
      JOIN schedule_orders so ON so."Id" = no."ScheduleOrderId"
      JOIN schedules s ON s."Id" = so."ScheduleId"
      WHERE s."ReadyForProduction" = true
  );

  -- Remove the OrderItems and NestOrders for the released schedule
  DELETE FROM order_items
  WHERE "NestOrderId" IN (
      SELECT no."Id" FROM nest_orders no
      JOIN schedule_orders so ON so."Id" = no."ScheduleOrderId"
      JOIN schedules s ON s."Id" = so."ScheduleId"
      WHERE s."ReadyForProduction" = true
  );

  DELETE FROM nest_orders
  WHERE "ScheduleOrderId" IN (
      SELECT so."Id" FROM schedule_orders so
      JOIN schedules s ON s."Id" = so."ScheduleId"
      WHERE s."ReadyForProduction" = true
  );

  -- Clean up orphaned parts
  DELETE FROM parts
  WHERE "Id" NOT IN (SELECT "PartId" FROM order_items)
    AND "Id" NOT IN (SELECT "PartId" FROM batch_items);

  -- Clean up nests that have no remaining nested_parts
  DELETE FROM nests
  WHERE "Id" NOT IN (SELECT "NestId" FROM nested_parts);

  -- Reset the schedule so it can be re-released
  UPDATE schedules SET "ReadyForProduction" = false WHERE "ReadyForProduction" = true;