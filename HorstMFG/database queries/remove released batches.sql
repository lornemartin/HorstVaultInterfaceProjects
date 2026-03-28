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

  -- Reset the batch so it can be re-released
  UPDATE batches SET "ReadyForProduction" = false WHERE "ReadyForProduction" = true;
  