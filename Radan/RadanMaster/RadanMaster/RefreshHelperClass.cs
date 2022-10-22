using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Columns;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RadanMaster
{
    public class RefreshHelper
    {
        [Serializable]
        public struct RowInfo
        {
            public object Id;
            public int level;
        };

        private GridView view;
        private string keyFieldName;

        public RefreshHelper(GridView view, string keyFieldName)
        {
            this.view = view;
            this.keyFieldName = keyFieldName;
        }

        protected int FindParentRowHandle(RowInfo rowInfo, int rowHandle)
        {
            int result = view.GetParentRowHandle(rowHandle);
            while (view.GetRowLevel(result) != rowInfo.level)
                result = view.GetParentRowHandle(result);
            return result;
        }

        protected void ExpandRowByRowInfo(RowInfo rowInfo)
        {
            int dataRowHandle = view.LocateByValue(0, view.Columns[keyFieldName], rowInfo.Id);
            if (dataRowHandle != GridControl.InvalidRowHandle)
            {
                int parentRowHandle = FindParentRowHandle(rowInfo, dataRowHandle);
                view.SetRowExpanded(parentRowHandle, true, false);
            }
        }


        public void SaveExpansionViewInfo()
        {
            if (view.GroupedColumns.Count == 0) return;

            ArrayList list = new ArrayList();
            GridColumn column = view.Columns[keyFieldName];
            for (int i = -1; i > int.MinValue; i--)
            {
                if (!view.IsValidRowHandle(i)) break;
                if (view.GetRowExpanded(i))
                {
                    RowInfo rowInfo;
                    int dataRowHandle = view.GetDataRowHandleByGroupRowHandle(i);
                    rowInfo.Id = view.GetRowCellValue(dataRowHandle, column);
                    rowInfo.level = view.GetRowLevel(i);
                    list.Add(rowInfo);
                }
            }

            string SettingsFilePath = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + @"\RadanMaster\ExapandedRows.xml";
            var serializer = new XmlSerializer(typeof(ArrayList), new Type[] { typeof(RowInfo) });
            using (FileStream stream = new FileStream(SettingsFilePath, FileMode.Create))
            {
                serializer.Serialize(stream, list);
            }
        }

        public void LoadExpansionViewInfo()
        {
            string SettingsFilePath = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) + @"\RadanMaster\ExapandedRows.xml";
            ArrayList list = new ArrayList();
            var serializer = new XmlSerializer(typeof(ArrayList), new Type[] { typeof(RowInfo) });
            using (FileStream stream = new FileStream(SettingsFilePath, FileMode.Open))
            {
                list = (ArrayList)serializer.Deserialize(stream);
            }

            if (view.GroupedColumns.Count == 0) return;
            view.BeginUpdate();
            try
            {
                view.CollapseAllGroups();
                foreach (RowInfo info in list)
                    ExpandRowByRowInfo(info);
            }
            finally
            {
                view.EndUpdate();
            }
        }

        public void SaveViewInfo()
        {
            SaveExpansionViewInfo();
        }

        public void LoadViewInfo()
        {
            LoadExpansionViewInfo();
        }
    }
}
