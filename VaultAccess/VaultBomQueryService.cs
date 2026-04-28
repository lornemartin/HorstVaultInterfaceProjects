using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ACW = Autodesk.Connectivity.WebServices;
using VDF = Autodesk.DataManagement.Client.Framework;

namespace VaultAccess
{
    public class VaultBomItem
    {
        public string Level { get; set; } = "";
        public string Number { get; set; } = "";
        public string Title { get; set; } = "";
        public string ItemDescription { get; set; } = "";
        public string Category { get; set; } = "";
        public string Thickness { get; set; } = "";
        public string Material { get; set; } = "";
        public string Operations { get; set; } = "";
        public int Qty { get; set; }
        public string StructCode { get; set; } = "";
        public string PlantId { get; set; } = "";
        public bool IsStock { get; set; }
        public bool RequiresPdf { get; set; }
        public string Comment { get; set; } = "";
        public string DateModified { get; set; } = "";
        public string LifecycleState { get; set; } = "";
        public string StockName { get; set; } = "";
        public string Keywords { get; set; } = "";
        public string Notes { get; set; } = "";
        public string Revision { get; set; } = "";
    }

    public class VaultBomResult
    {
        public string ItemNumber { get; set; } = "";
        public List<VaultBomItem> Lines { get; set; } = new List<VaultBomItem>();
    }

    public class VaultBomQueryService
    {
        private readonly VaultAccess _vault;
        private readonly VDF.Vault.Currency.Connections.Connection _conn;

        public VaultBomQueryService(VaultAccess vault, VDF.Vault.Currency.Connections.Connection conn)
        {
            _vault = vault;
            _conn = conn;
        }

        /// <summary>
        /// Looks up a Vault item by item number and returns its BOM as a flat list of rows.
        /// Returns null if the item does not exist in Vault.
        /// </summary>
        /// <param name="refreshFromSource">When true, runs the promote-components routine on the
        /// top-level item and every BOM child before extracting data. Mirrors what ItemExport's
        /// BomItemExportCommandHandler does so exported properties are guaranteed current.</param>
        public VaultBomResult GetItemBom(string itemNumber, bool refreshFromSource)
        {
            var itemSvc = _conn.WebServiceManager.ItemService;
            var pkgSvc = _conn.WebServiceManager.PackageService;
            var docSvc = _conn.WebServiceManager.DocumentService;

            ACW.Item topItem;
            try
            {
                topItem = itemSvc.GetLatestItemByItemNumber(itemNumber);
            }
            catch
            {
                return null;
            }
            if (topItem == null) return null;

            if (refreshFromSource)
            {
                _vault.UpdateItem(topItem, _conn);
                ACW.ItemAssoc[] childAssocs = null;
                try
                {
                    childAssocs = itemSvc.GetItemBOMAssociationsByItemIds(new long[] { topItem.Id }, false);
                }
                catch { }
                if (childAssocs != null)
                {
                    foreach (var assoc in childAssocs)
                    {
                        var children = itemSvc.GetItemsByIds(new long[] { assoc.CldItemID });
                        if (children != null && children.Length > 0 && children[0] != null)
                            _vault.UpdateItem(children[0], _conn);
                    }
                }
            }

            ACW.PkgItemsAndBOM pkgBom = pkgSvc.GetLatestPackageDataByItemIds(
                new long[] { topItem.Id }, ACW.BOMTyp.Latest);

            ACW.MapPair[] mappings = BuildBomMappings();

            ACW.FileNameAndURL fileNameAndUrl = pkgSvc.ExportToPackage(
                pkgBom, ACW.FileFormat.TDL_LEVEL, mappings);

            byte[] tsvBytes = DownloadAllBytes(pkgSvc, fileNameAndUrl);

            Dictionary<string, string> fileNameByItemNum = BuildFileNameLookup(pkgBom, itemSvc, docSvc);

            return new VaultBomResult
            {
                ItemNumber = itemNumber,
                Lines = ParseTsv(tsvBytes, fileNameByItemNum)
            };
        }

        // Field mappings mirror ExportVaultItemsByBatch in VaultAccess.cs — keep in sync.
        private static ACW.MapPair[] BuildBomMappings()
        {
            return new ACW.MapPair[]
            {
                new ACW.MapPair { ToName = "Parent",           FromName = "BOMStructure-41FF056B-8EEF-47E2-8F9E-490BC0C52C71" },
                new ACW.MapPair { ToName = "Number",           FromName = "Number" },
                new ACW.MapPair { ToName = "Title (Item,CO)",  FromName = "Title(Item,CO)" },
                new ACW.MapPair { ToName = "Item Description", FromName = "Description(Item,CO)" },
                new ACW.MapPair { ToName = "CategoryName",     FromName = "CategoryName" },
                new ACW.MapPair { ToName = "Thickness",        FromName = "7c5169ad-9081-4aa7-b1a3-4670edae0b8c" },
                new ACW.MapPair { ToName = "Material",         FromName = "Material" },
                new ACW.MapPair { ToName = "Operations",       FromName = "794d5b7d-49b5-49ba-938a-7e341a7ff8e4" },
                new ACW.MapPair { ToName = "Quantity",         FromName = "Quantity-41FF056B-8EEF-47E2-8F9E-490BC0C52C71" },
                new ACW.MapPair { ToName = "Structural Code",  FromName = "e3811c7a-a3ee-4f67-b34e-cbc892640616" },
                new ACW.MapPair { ToName = "Plant ID",         FromName = "eff195ae-da71-4929-b3df-2d6fd1e25f53" },
                new ACW.MapPair { ToName = "Is Stock",         FromName = "f78c17cd-86d1-4728-96b5-8001fb58b67f" },
                new ACW.MapPair { ToName = "Requires PDF",     FromName = "6df4ae8b-fbd9-4e62-b801-a46097d4f9c5" },
                new ACW.MapPair { ToName = "Comment",          FromName = "Comment" },
                new ACW.MapPair { ToName = "Date Modified",    FromName = "ModDate" },
                new ACW.MapPair { ToName = "State",            FromName = "State" },
                new ACW.MapPair { ToName = "Stock Name",       FromName = "a42ca550-c503-4835-99dd-8c4d4ff6dbaf" },
                new ACW.MapPair { ToName = "Keywords",         FromName = "Keywords" },
                new ACW.MapPair { ToName = "Notes",            FromName = "0d012a5c-cc28-443c-b44e-735372eee117" },
                new ACW.MapPair { ToName = "Revision",         FromName = "Revision" },
            };
        }

        private static byte[] DownloadAllBytes(ACW.PackageService pkgSvc, ACW.FileNameAndURL fileNameAndUrl)
        {
            using (var ms = new MemoryStream())
            {
                long currentByte = 0;
                long partSize = 1024L * 1024L; // 1 MB chunks
                while (currentByte < fileNameAndUrl.FileSize)
                {
                    long lastByte = currentByte + partSize < fileNameAndUrl.FileSize
                        ? currentByte + partSize
                        : fileNameAndUrl.FileSize;
                    byte[] chunk = pkgSvc.DownloadPackagePart(fileNameAndUrl.Name, currentByte, lastByte);
                    ms.Write(chunk, 0, (int)(lastByte - currentByte));
                    currentByte += partSize;
                }
                return ms.ToArray();
            }
        }

        // Maps each Vault item number in the BOM to the primary linked file's name (e.g. "Bracket.ipt").
        // Top-level items have XRefId == -1 and are intentionally absent from the result so callers
        // keep the original item number for them.
        private static Dictionary<string, string> BuildFileNameLookup(
            ACW.PkgItemsAndBOM pkgBom,
            ACW.ItemService itemSvc,
            ACW.DocumentService docSvc)
        {
            var result = new Dictionary<string, string>();
            if (pkgBom.PkgItemArray == null || pkgBom.PkgItemArray.Length == 0)
                return result;

            var idList = new List<long>();
            var idByItemNum = new Dictionary<string, long>();
            foreach (var v in pkgBom.PkgItemArray)
            {
                idList.Add(v.ID);
                if (!idByItemNum.ContainsKey(v.ItemNum))
                    idByItemNum.Add(v.ItemNum, v.ID);
            }

            ACW.BOMComp[] bomComps = itemSvc.GetPrimaryComponentsByItemIds(idList.ToArray());
            var bomCompById = new Dictionary<long, ACW.BOMComp>();
            for (int i = 0; i < idList.Count && i < bomComps.Length; i++)
            {
                bomCompById[idList[i]] = bomComps[i];
            }

            foreach (var kv in idByItemNum)
            {
                if (!bomCompById.TryGetValue(kv.Value, out var bomComp)) continue;
                if (bomComp == null || bomComp.XRefId == -1) continue;

                try
                {
                    var file = docSvc.GetFileById(bomComp.XRefId);
                    if (file != null && !string.IsNullOrEmpty(file.Name))
                        result[kv.Key] = file.Name;
                }
                catch { }
            }

            return result;
        }

        // Column order matches BuildBomMappings: Parent, Number, Title, Description, Category,
        // Thickness, Material, Operations, Qty, StructCode, PlantId, IsStock, RequiresPdf,
        // Comment, ModDate, State, StockName, Keywords, Notes, Revision.
        private static List<VaultBomItem> ParseTsv(byte[] tsvBytes, Dictionary<string, string> fileNameByItemNum)
        {
            var result = new List<VaultBomItem>();
            using (var ms = new MemoryStream(tsvBytes))
            using (var reader = new StreamReader(ms))
            {
                reader.ReadLine(); // header
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Replace("\"", "");
                    var cols = line.Split('\t');
                    if (cols.Length < 19) continue;

                    string origNumber = cols[1];
                    string displayNumber = origNumber;
                    if (fileNameByItemNum.TryGetValue(origNumber, out var fileName))
                        displayNumber = fileName;

                    int qty = 1;
                    if (int.TryParse(cols[8], out var q) && q > 0) qty = q;

                    result.Add(new VaultBomItem
                    {
                        Level           = cols[0],
                        Number          = displayNumber,
                        Title           = cols[2],
                        ItemDescription = cols[3],
                        Category        = cols[4],
                        Thickness       = cols[5],
                        Material        = cols[6],
                        Operations      = cols[7],
                        Qty             = qty,
                        StructCode      = cols[9],
                        PlantId         = cols[10],
                        IsStock         = cols[11].Equals("True", StringComparison.OrdinalIgnoreCase),
                        RequiresPdf     = !cols[12].Equals("False", StringComparison.OrdinalIgnoreCase),
                        Comment         = cols[13],
                        DateModified    = cols[14],
                        LifecycleState  = cols[15],
                        StockName       = cols[16],
                        Keywords        = cols[17],
                        Notes           = cols[18],
                        Revision        = cols.Length > 19 ? cols[19] : "",
                    });
                }
            }
            return result;
        }
    }
}
