using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SqlScript2PdmConsoleApp
{
    class Program
    {

        static void Main(string[] args)
        {

            string currPath = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                currPath = string.Format(@".\");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                currPath = string.Format(@"./");
            }
            
            var sqlFileList = Directory.GetFiles(currPath, "*.sql");
            if (sqlFileList == null || sqlFileList.Count() <= 0)
            {
                Console.WriteLine("not exists sql files");
            }

            foreach (var item in sqlFileList)
            {
                FileInfo file = new FileInfo(item);
                IList<string> list = GetSqlScriptLines(item);
                var tables = SqlScriptConvert(list);
                var maxHeight = 0;
                for (int i = 1; i <= tables.Keys.Count; i++)
                {
                    var rect = new TableRect();
                    if (i == 1)
                    {
                        rect.s_x = 0;
                        rect.s_y = 0;
                        rect.e_x = 15000;
                        rect.e_y = 14000;
                    }
                    else if (i == 2)
                    {
                        rect.s_x = tables[i - 1].TableRect.s_x + (15000 + 5000);
                        rect.s_y = tables[i - 1].TableRect.s_y + 0;
                        rect.e_x = rect.s_x + 15000;
                        rect.e_y = rect.s_y + 14000;
                    }
                    else if (i % 2 == 1)
                    {
                        rect.s_x = tables[i - 2].TableRect.s_x - (15000 + 5000);
                        rect.s_y = tables[i - 2].TableRect.s_y - 0;
                        rect.e_x = rect.s_x + 15000;
                        rect.e_y = rect.s_y + 14000;
                    }
                    else if (i % 2 == 0)
                    {
                        rect.s_x = tables[i - 2].TableRect.s_x + (15000 + 5000);
                        rect.s_y = tables[i - 2].TableRect.s_y + 0;
                        rect.e_x = rect.s_x + 15000;
                        rect.e_y = rect.s_y + 14000;
                    }

                    if (rect.s_x > 460000)   //860000
                    {
                        rect.s_x = 0;
                        rect.s_y = 0 - (maxHeight + 5000);
                        rect.e_x = 0 - 15000;
                        rect.e_y = 0 - (14000 + maxHeight + 5000);
                        maxHeight = 0;
                    }
                    if (rect.s_x < -480000) //880000
                    {
                        rect.s_x = 0 - 15000 - 5000;
                        rect.s_y = 0 - (maxHeight + 5000);
                        rect.e_x = 0 - 15000;
                        rect.e_y = 0 - (14000 + maxHeight + 5000);
                        maxHeight = 0;
                    }
                    
                    if (tables[i].ColumnList.Count * 1000 > maxHeight)
                    {
                        maxHeight = tables[i].ColumnList.Count * 1000;
                    }
                    if (Math.Abs(rect.e_y) > maxHeight)
                    {
                        maxHeight = Math.Abs(rect.e_y);
                    }
                    tables[i].TableRect = rect;
                    tables[i].TableRectStr = string.Format("(({0},{1}), ({2},{3}))", rect.s_x, rect.s_y, rect.e_x, rect.e_y);
                    //Console.WriteLine(tables[i].TableName);
                }

                PDM_Convert(tables, Path.GetFileNameWithoutExtension(file.FullName));
            }
            Console.WriteLine("it's ok!");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        static IList<string> GetSqlScriptLines(string sqlFiles)
        {
            string sqlFilePath = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                sqlFilePath = string.Format(@".\{0}", sqlFiles);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                sqlFilePath = string.Format(@"./{0}", sqlFiles);
            }
            TxtHelper.filePath = sqlFilePath;
            IList<string> list = TxtHelper.ReadTextByLine();
            return list;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        static IDictionary<int, TableModel> SqlScriptConvert(IList<string> list)
        {
            bool isInTable = false;
            int tableID = 0;
            int columnID = 0;
            IDictionary<int, TableModel> tableDic = new Dictionary<int, TableModel>();
            for (int i = 0; i < list.Count; i++)
            {
                if (string.IsNullOrEmpty(list[i].Trim()))
                { }
                else if (list[i].Trim().StartsWith("--"))
                { }
                else if (list[i].Trim().StartsWith(@"/*"))
                {
                    if (list[i].Contains(@"*/"))
                    { }
                    else
                    {
                        for (int j = i + 1; j < list.Count; j++)
                        {
                            if (list[j].Contains(@"*/"))
                            {
                                i = j;
                                break;
                            }
                        }
                    }
                }
                else if ((list[i].Trim().ToLower().StartsWith(@"constraint") || list[i].Trim().ToLower().StartsWith(@"primary")) && list[i].Trim().ToLower().Contains("primary"))
                {
                    var mainIndex = list[i].Trim().ToLower().IndexOf("primary");
                    var mainStr = list[i].Trim().ToLower().Substring(mainIndex, list[i].Trim().Length - mainIndex);
                    //mainStr = mainStr.Replace("primary", "").Replace("key", "").Replace("(", "").Replace(")", "").Trim();
                    mainStr = mainStr.Substring(mainStr.IndexOf("(") + 1, mainStr.IndexOf(")") - mainStr.IndexOf("(") - 1).Trim();
                    if (mainStr.Split(',').Length > 1)
                    {
                        foreach (var key in mainStr.Split(','))
                        {
                            tableDic[tableID].ColumnList.ToList().Where(a => a.ColName.ToLower().Equals(key)).FirstOrDefault().ColIsMain = true;
                        }
                    }
                    else
                    {
                        tableDic[tableID].ColumnList.ToList().Where(a => a.ColName.ToLower().Equals(mainStr)).FirstOrDefault().ColIsMain = true;
                    }
                }
                else if (list[i].Trim().StartsWith(@");"))
                {
                    isInTable = false;
                }
                else
                {
                    if (list[i].Trim().ToLower().StartsWith("create") && list[i].Trim().ToLower().Contains("table"))
                    {
                        isInTable = true;
                        tableID++;
                        var tableRow = list[i].Trim().Split(' ')[2].Replace("(", "");
                        if (tableRow.Contains("."))
                        {
                            tableDic.Add(tableID, new TableModel()
                            {
                                TableID = tableID,
                                TableName = tableRow.Split(',')[1],
                                ColumnList = new List<ColumnModel>()
                            });
                        }
                        else
                        {
                            tableDic.Add(tableID, new TableModel()
                            {
                                TableID = tableID,
                                TableName = tableRow,
                                ColumnList = new List<ColumnModel>()
                            });
                        }
                    }
                    else if (isInTable && (short)list[i].Trim().ToLower().Substring(0, 1).ToCharArray()[0] >= (short)'a' && (short)(list[i].Trim().ToLower().Substring(0, 1).ToCharArray()[0]) <= (short)'z')
                    {
                        columnID++;
                        //SqlScriptConvert_Column(tableID, columnID, list[i], tableDic);
                        SqlScriptConvert_Column2(tableID, columnID, list[i], tableDic);
                    }
                }
            }
            return tableDic;
        }

        static void SqlScriptConvert_Column(int tableID, int columnID, string list_i_str, IDictionary<int, TableModel> tableDic)
        {
            var tempstr = list_i_str.Trim();
            ColumnModel col = new ColumnModel()
            {
                ColumnID = columnID,
                ColName = tempstr.Split(' ')[0],
                ColDefault = "",
                ColComment = ""
            };

            #region 字段类型
            //tempstr = tempstr.Replace(col.ColName, "").Trim();
            tempstr = tempstr.Substring(col.ColName.Length).Trim().ToLower();
            var typeTempStr = string.Empty;
            if (tempstr.StartsWith("varchar") || tempstr.StartsWith("nvarchar") || tempstr.StartsWith("char") || tempstr.StartsWith("nchar")
                || tempstr.StartsWith("character"))
            {
                var colTpeIndex = tempstr.IndexOf(")");
                var typeStr = tempstr.Substring(0, colTpeIndex + 1);
                typeTempStr = typeStr;
                col.ColType = typeStr.Substring(0, typeStr.IndexOf("("));
                col.ColLength = typeStr.Substring(typeStr.IndexOf("(") + 1, typeStr.IndexOf(")") - typeStr.IndexOf("(") - 1);
            }
            else if (tempstr.StartsWith("decimal") || tempstr.StartsWith("float") || tempstr.StartsWith("double") || tempstr.StartsWith("numeric"))
            {
                var colTpeIndex = tempstr.IndexOf(")");
                var typeStr = tempstr.Substring(0, colTpeIndex + 1);
                typeTempStr = typeStr;
                col.ColType = typeStr.Substring(0, typeStr.IndexOf("("));
                var dd = typeStr.Substring(typeStr.IndexOf("(") + 1, typeStr.IndexOf(")") - typeStr.IndexOf("(") - 1);
                col.ColPrecision = dd.Split(',')[0];
                col.ColScale = dd.Split(',')[1];
            }
            else if (tempstr.StartsWith("time") || tempstr.StartsWith("timestamp") || tempstr.StartsWith("interval"))
            {
                var colTpeIndex = tempstr.IndexOf(" ");
                col.ColType = tempstr.Substring(0, colTpeIndex + 1);
                typeTempStr = col.ColType;
            }
            else
            {
                var colTpeIndex = tempstr.IndexOf(" ");
                col.ColType = tempstr.Substring(0, colTpeIndex + 1);
                typeTempStr = col.ColType;
            }
            //tempstr = tempstr.Replace(typeTempStr, "").Trim();
            tempstr = tempstr.Substring(typeTempStr.Length).Trim();
            #endregion

            var tempArray = tempstr.Split(",");
            tempstr = tempArray[0];

            #region comment
            if (tempArray[1].Trim().StartsWith("--"))
            {
                col.ColComment = tempArray[1].Trim().Substring(tempArray[1].Trim().IndexOf("--") + 2).Trim();
            }
            #endregion

            #region null default 
            // default  null
            if (tempstr.StartsWith("default") && (tempstr.EndsWith("null") || tempstr.EndsWith(",")))
            {
                var colTpeIndex = tempstr.IndexOf(")");
                if (colTpeIndex <= 0)
                {
                    colTpeIndex = tempstr.IndexOf("null");
                    if (colTpeIndex <= 0)
                    {
                        colTpeIndex = tempstr.IndexOf("not");
                    }
                    col.ColDefault = tempstr.Substring(0, colTpeIndex);
                }
                else
                {
                    col.ColDefault = tempstr.Substring(0, colTpeIndex + 1);
                }
                //tempstr = tempstr.Replace(col.ColDefault, "").Trim();
                tempstr = tempstr.Substring(col.ColDefault.Length);
                col.ColDefault = col.ColDefault.Replace("default", "").Trim();
            }
            if (tempstr.StartsWith("not") || tempstr.StartsWith("null"))
            {
                if (tempstr.Contains("default"))
                {
                    var colTpeIndex = tempstr.IndexOf("default");
                    var nullStr = tempstr.Substring(0, colTpeIndex + 1);
                    col.ColIsNull = nullStr.Contains("not") ? false : true;
                    tempstr = tempstr.Substring(tempstr.IndexOf("default") + 7).Trim();
                    col.ColDefault = tempstr;
                }
                else if (tempstr.EndsWith(","))
                {
                    col.ColIsNull = tempstr.Contains("not") ? false : true;
                }
            }
            #endregion
            tableDic[tableID].ColumnList.Add(col);
        }

        static void SqlScriptConvert_Column2(int tableID, int columnID, string list_i_str, IDictionary<int, TableModel> tableDic)
        {
            ColumnModel col = new ColumnModel()
            {
                ColumnID = columnID,
                ColName = "",
                ColDefault = "",
                ColComment = ""
            };

            string tempstr = string.Empty;
            for (int i = 0; i < list_i_str.Length; i++)
            {
                if (i < list_i_str.Length - 1 && list_i_str[i].Equals('-') && list_i_str[i + 1].Equals('-'))
                {
                    col.ColComment = list_i_str.Substring(i + 2).Trim();
                    break;
                }
                else if (!list_i_str[i].Equals(' '))
                {
                    tempstr = string.Format("{0}{1}", tempstr, list_i_str[i]);
                }
                else if (!string.IsNullOrEmpty(tempstr))
                {
                    if (tempstr.Contains("'") && tempstr.Split("'").Length % 2 == 0)
                    {
                        continue;
                    }
                    else if (!tempstr.Contains("'") && tempstr.Contains("(") && tempstr.Split("(").Length != tempstr.Split(")").Length)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(col.ColName))
                    {
                        col.ColName = tempstr;
                    }
                    else if (string.IsNullOrEmpty(col.ColTypeStr))
                    {
                        col.ColTypeStr = tempstr;
                        if (col.ColTypeStr.Contains("("))
                        {
                            if (col.ColTypeStr.Contains(","))
                            {
                                col.ColType = col.ColTypeStr.Substring(0, col.ColTypeStr.IndexOf("("));
                                col.ColLength = col.ColTypeStr.Substring(col.ColTypeStr.IndexOf("(") + 1, col.ColTypeStr.IndexOf(")") - col.ColTypeStr.IndexOf("(") - 1);
                                col.ColPrecision = col.ColLength.Split(',')[0];
                                col.ColScale = col.ColLength.Split(',')[1];
                            }
                            else
                            {
                                col.ColType = col.ColTypeStr.Substring(0, col.ColTypeStr.IndexOf("("));
                                col.ColLength = col.ColTypeStr.Substring(col.ColTypeStr.IndexOf("(") + 1, col.ColTypeStr.IndexOf(")") - col.ColTypeStr.IndexOf("(") - 1);
                            }
                        }
                        else
                        {
                            col.ColType = col.ColTypeStr;
                        }
                    }
                    else if (tempstr.ToLower().StartsWith("default") && tempstr.Length <= "default".Length)
                    {
                        tempstr = tempstr + " ";
                        continue;
                    }
                    else if (tempstr.ToLower().StartsWith("default") && string.IsNullOrEmpty(col.ColDefaultStr))
                    {
                        col.ColDefaultStr = tempstr;
                        col.ColDefault = col.ColDefaultStr.Substring("default".Length);
                    }
                    else if (tempstr.ToLower().StartsWith("not") && !tempstr.EndsWith("null"))
                    {
                        tempstr = tempstr + " ";
                        continue;
                    }
                    else if (tempstr.EndsWith("null") && string.IsNullOrEmpty(col.ColIsNullStr))
                    {
                        col.ColIsNullStr = tempstr;
                        if (col.ColIsNullStr.ToLower().Contains("not"))
                        {
                            col.ColIsNull = false;
                        }
                    }
                    //Console.WriteLine(tempstr);
                    tempstr = string.Empty;
                }
                else
                {
                    continue;
                }
            }
            //Console.WriteLine("   " + col.ColName + "  " + col.ColType + "    " + col.ColLength + "    " + col.ColPrecision + "    " + col.ColScale + "   " + col.ColDefault + "    " + col.ColIsNull + "    " + col.ColComment);
            tableDic[tableID].ColumnList.Add(col);
        }

        static void PDM_Convert(IDictionary<int, TableModel> tables, string databaseName)
        {
            string templatePath = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                templatePath = string.Format(@".\{0}", "pg_pdm_template.pdm");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                templatePath = string.Format(@"./{0}", "pg_pdm_template.pdm");
            }
            TxtHelper.filePath = templatePath;
            string xml = TxtHelper.ReadTxt();
            xml = xml.Replace("pdm_object_name", databaseName + "_pdm");
            xml = xml.Replace("pdm_physical_name", databaseName + "_physical_pdm");
            xml = xml.Replace("pdm_model_classid", Guid.NewGuid().ToString());
            xml = xml.Replace("pdm_target_modelid", Guid.NewGuid().ToString());
            xml = xml.Replace("pdm_objectid", Guid.NewGuid().ToString());

            string pdmTableSymbolTemplate = @"
<o:TableSymbol Id=""TableSymbolID"">
<a:CreationDate>1544667540</a:CreationDate>
<a:ModificationDate>1544668865</a:ModificationDate>
<a:IconMode>-1</a:IconMode>
<a:Rect>pdmTableSymbolRect</a:Rect>
<a:LineColor>12615680</a:LineColor>
<a:FillColor>16570034</a:FillColor>
<a:ShadowColor>12632256</a:ShadowColor>
<a:FontList>STRN 0 新宋体,8,N
DISPNAME 0 新宋体,8,N
OWNRDISPNAME 0 新宋体,8,N
Columns 0 新宋体,8,N
TablePkColumns 0 新宋体,8,U
TableFkColumns 0 新宋体,8,N
Keys 0 新宋体,8,N
Indexes 0 新宋体,8,N
Triggers 0 新宋体,8,N
LABL 0 新宋体,8,N</a:FontList>
<a:BrushStyle>6</a:BrushStyle>
<a:GradientFillMode>65</a:GradientFillMode>
<a:GradientEndColor>16777215</a:GradientEndColor>
<c:Object>
<o:Table Ref=""TableID""/>
</c:Object>
</o:TableSymbol>
";
            StringBuilder PDMTableSymbol_sb = new StringBuilder();
            foreach (var item in tables.Values)
            {
                var symbol = pdmTableSymbolTemplate;
                symbol = symbol.Replace("TableSymbolID", "o" + (item.TableID + 1000).ToString());
                symbol = symbol.Replace("pdmTableSymbolRect", item.TableRectStr);
                symbol = symbol.Replace("TableID", "o" + (item.TableID + 2000).ToString());
                PDMTableSymbol_sb.Append(symbol);
            }
            xml = xml.Replace("PDMTableSymbol", PDMTableSymbol_sb.ToString());

            string pdmKeyTemplate = @"
<o:Key Id=""KeyID"">
<a:ObjectID>KeyObjectID</a:ObjectID>
<a:Name>Key_1</a:Name>
<a:Code>Key_1</a:Code>
<a:CreationDate>1544668941</a:CreationDate>
<a:Creator>Administrator</a:Creator>
<a:ModificationDate>1544668987</a:ModificationDate>
<a:Modifier>Administrator</a:Modifier>
<c:Key.Columns>
<o:Column Ref=""KeyColumnID""/>
</c:Key.Columns>
</o:Key>
";
            string pdmTableTemplate = @"
<o:Table Id=""TableID"">
<a:ObjectID>TableObjectID</a:ObjectID>
<a:Name>TableName</a:Name>
<a:Code>TableName</a:Code>
<a:CreationDate>1544667539</a:CreationDate>
<a:Creator>Administrator</a:Creator>
<a:ModificationDate>1544668987</a:ModificationDate>
<a:Modifier>Administrator</a:Modifier>
<a:TotalSavingCurrency/>
<c:Columns>
pdm_table_columns
</c:Columns>
<c:Keys>
pdm_table_keys
</c:Keys>
<c:PrimaryKey>
pdm_table_primary_key
</c:PrimaryKey>
</o:Table>
";
            StringBuilder PDMTABLES_sb = new StringBuilder();
            foreach (var item in tables.Values)
            {
                StringBuilder pdmtablecolumns_sb = new StringBuilder();
                foreach (var jj in item.ColumnList)
                {
                    StringBuilder columnstr_sb = new StringBuilder();
                    columnstr_sb.Append("<o:Column Id=\"" + "o" + (jj.ColumnID + 3000).ToString() + "\">\r\n");
                    columnstr_sb.Append("<a:ObjectID>" + jj.ObjectID.ToString() + "</a:ObjectID>\r\n");
                    columnstr_sb.Append("<a:Name>" + jj.ColName.ToString() + "</a:Name>\r\n");
                    columnstr_sb.Append("<a:Code>" + jj.ColName.ToString() + "</a:Code>\r\n");
                    columnstr_sb.Append("<a:CreationDate>1544667543</a:CreationDate>\r\n");
                    columnstr_sb.Append("<a:Creator>Administrator</a:Creator>\r\n");
                    columnstr_sb.Append("<a:ModificationDate>1544668987</a:ModificationDate>\r\n");
                    columnstr_sb.Append("<a:Modifier>Administrator</a:Modifier>\r\n");

                    if (!string.IsNullOrEmpty(jj.ColComment))
                    {
                        columnstr_sb.Append("<a:Comment>" + jj.ColComment + "</a:Comment>\r\n");
                    }
                    if (!string.IsNullOrEmpty(jj.ColDefault))
                    {
                        columnstr_sb.Append("<a:DefaultValue>" + jj.ColDefault + "</a:DefaultValue>\r\n");
                    }
                    columnstr_sb.Append("<a:DataType>" + jj.ColTypeStr.ToString() + "</a:DataType>\r\n");
                    if (!string.IsNullOrEmpty(jj.ColLength))
                    {
                        columnstr_sb.Append("<a:Length>" + jj.ColLength.ToString() + "</a:Length>\r\n");
                    }
                    columnstr_sb.Append("<a:Column.Mandatory>" + (jj.ColIsNull ? 0 : 1).ToString() + "</a:Column.Mandatory>\r\n");
                    columnstr_sb.Append("</o:Column>\r\n");

                    pdmtablecolumns_sb.Append(columnstr_sb.ToString());
                }

                var tableModel = pdmTableTemplate;
                tableModel = tableModel.Replace("TableID", "o" + (item.TableID + 2000).ToString());
                tableModel = tableModel.Replace("TableObjectID", item.ObjectID.ToString());
                tableModel = tableModel.Replace("TableName", item.TableName.ToString());
                tableModel = tableModel.Replace("pdm_table_columns", pdmtablecolumns_sb.ToString());

                if (item.ColumnList.Where(a => a.ColIsMain).Count() > 0)
                {
                    var tableKey = item.ColumnList.Where(a => a.ColIsMain).FirstOrDefault();
                    var keyModel = pdmKeyTemplate;
                    keyModel = keyModel.Replace("KeyID", "o" + (tableKey.ColumnID + 4000).ToString());
                    keyModel = keyModel.Replace("KeyObjectID", Guid.NewGuid().ToString());
                    keyModel = keyModel.Replace("KeyColumnID", "o" + (tableKey.ColumnID + 3000).ToString());

                    tableModel = tableModel.Replace("pdm_table_keys", keyModel);
                    tableModel = tableModel.Replace("pdm_table_primary_key", "<o:Key Ref=\"" + "o" + (tableKey.ColumnID + 4000).ToString() + "\"/>");
                }
                else
                {
                    tableModel = tableModel.Replace("pdm_table_keys", "");
                    tableModel = tableModel.Replace("pdm_table_primary_key", "");
                }
                PDMTABLES_sb.Append(tableModel);

            }
            xml = xml.Replace("PDMTABLES", PDMTABLES_sb.ToString());

            string savePdmPath = string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                savePdmPath = string.Format(@".\{0}", databaseName + ".pdm");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                savePdmPath = string.Format(@"./{0}", databaseName + ".pdm");
            }
            TxtHelper.WriteTxt(xml, savePdmPath, FileMode.OpenOrCreate);

        }


    }

    public class TableModel
    {
        public int TableID { get; set; }
        public Guid ObjectID { get { return Guid.NewGuid(); } }
        public string TableName { get; set; }
        public TableRect TableRect { get; set; }
        public string TableRectStr { get; set; }
        public IList<ColumnModel> ColumnList { get; set; }
    }
    public class TableRect
    {
        public int s_x { get; set; }
        public int s_y { get; set; }
        public int e_x { get; set; }
        public int e_y { get; set; }
    }
    public class ColumnModel
    {
        public int ColumnID { get; set; }
        public Guid ObjectID { get { return Guid.NewGuid(); } }
        public string ColName { get; set; }
        public string ColTypeStr { get; set; }
        public string ColType { get; set; }
        public string ColLength { get; set; }
        public string ColPrecision { get; set; }
        public string ColScale { get; set; }
        public string ColDefaultStr { get; set; }
        public string ColDefault { get; set; }
        public string ColIsNullStr { get; set; }
        public bool ColIsNull { get; set; }
        public bool ColIsMain { get; set; }
        public string ColComment { get; set; }
    }
}
