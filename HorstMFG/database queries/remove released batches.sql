  -- Remove nested_parts referencing the batch items being deleted
  DELETE FROM nested_parts
  WHERE "BatchItemId" IN (
      SELECT bi."Id" FROM batch_items bi
      JOIN nest_batches nb ON nb."Id" = bi."NestBatchId"
      JOIN batches b ON b."Id" = nb."BatchId"
      WHERE b."ReadyForProduction" = true
  );

  -- Remove the BatchItems and NestBatch for the released batch
  DELETE FROM batch_items
  WHERE "NestBatchId" IN (
      SELECT nb."Id" FROM nest_batches nb
      JOIN batches b ON b."Id" = nb."BatchId"
      WHERE b."ReadyForProduction" = true
  );

  DELETE FROM nest_batches
  WHERE "BatchId" IN (SELECT "Id" FROM batches WHERE "ReadyForProduction" = true);

  -- Clean up orphaned parts
  DELETE FROM parts
  WHERE "Id" NOT IN (SELECT "PartId" FROM order_items)
    AND "Id" NOT IN (SELECT "PartId" FROM batch_items);

  -- Clean up nests that have no remaining nested_parts
  DELETE FROM nests
  WHERE "Id" NOT IN (SELECT "NestId" FROM nested_parts);

  -- Reset the batch so it can be re-released
  UPDATE batches SET "ReadyForProduction" = false WHERE "ReadyForProduction" = true;
  