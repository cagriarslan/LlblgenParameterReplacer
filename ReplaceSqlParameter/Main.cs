using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace ReplaceSqlParameter
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "ReplaceSqlParameter";
        static string iniFilePath = null;
        static string message = "Not a LLBLGEN Log";
        static bool someSetting = false;
        static frmMyDlg frmMyDlg = null;
        static int idMyDlg = -1;
        static Bitmap tbBmp = Properties.Resources.star;
        static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        static Icon tbIcon = null;
        #endregion

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");
            someSetting = (Win32.GetPrivateProfileInt("SomeSection", "SomeKey", 0, iniFilePath) != 0);       

            PluginBase.SetCommand(0, "Replace", prepareSqlAndShow, new ShortcutKey(true, false, true, Keys.Z));
            idMyDlg = 0;

        }
        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }
        internal static void PluginCleanUp()
        {
            Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);
        }
        #endregion

        #region " Menu functions "

        public static string GetDocumentText(IntPtr curScintilla)
        {
            string text = "";
            int start = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETSELECTIONNSTART, 0, 0);
            int end = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETSELECTIONNEND, 0, 0);
            int length = end - start + 1;
            if (length > 1)
            {
                StringBuilder sb = new StringBuilder(length);
                Win32.SendMessage(curScintilla, SciMsg.SCI_GETSELTEXT, 0, sb);
                text = sb.ToString();

            }
            else
            {
                length = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETTEXTLENGTH, 0, 0) + 1;
                StringBuilder sb = new StringBuilder(length);
                Win32.SendMessage(curScintilla, SciMsg.SCI_SETSELECTIONNSTART, 0, 0);
                Win32.SendMessage(curScintilla, SciMsg.SCI_SETSELECTIONNEND, 0, length);
                Win32.SendMessage(curScintilla, SciMsg.SCI_GETSELTEXT, 0, sb);
                text = sb.ToString();

            }

            return text.Trim();
        
        }

        public static void ShowDialog(string preparedSql)
        {
            if (frmMyDlg == null)
            {
                frmMyDlg = new frmMyDlg();

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 100, 100), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = frmMyDlg.Handle;
                _nppTbData.pszName = "Sql Output";
                _nppTbData.dlgID = idMyDlg;
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);


                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            }
            else
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, frmMyDlg.Handle);
            }
            Control ctn = (frmMyDlg.Controls.Find("textBox1", true))[0];
            ctn.Text = preparedSql;
            ctn.Focus();
            (ctn as TextBoxBase).SelectAll();

        }

        internal static void prepareSqlAndShow()
        {
            string rawText = GetDocumentText(PluginBase.GetCurrentScintilla());     
            List<Parameter> parameterList = new List<Parameter>();
            List<string> notifList = new List<string>();
            try
            {
                int parametreBaslangic = rawText.IndexOf("Parameter: :");
                String[] allStr;
                String query;
                if (parametreBaslangic < 0)
                {
                    parametreBaslangic = 0;
                    allStr = new string[] { };
                    query = rawText;
                }
                else
                {
                    allStr = Regex.Split(rawText.Substring(parametreBaslangic), "\n");
                    query = rawText.Substring(0, parametreBaslangic).Replace("\n", "").Replace(Environment.NewLine, "");
                }

                if ( query.IndexOf("Query:")>-1 )
                {
                    query = query.Substring(query.IndexOf("Query:"));   
                }

                if (query.Trim().StartsWith("Query:") || query.Trim().StartsWith("SELECT") || query.Trim().StartsWith("UPDATE") || query.Trim().StartsWith("INSERT"))
                {
                    if (query.Trim().StartsWith("Query:"))
                    {
                        query = query.Replace("Query:", "");
                    }
                    query = query.Replace("\"YNA\".", "");
                    query = query.Replace("\"", "");

                    int rowNum = 0;
                    foreach (String str in allStr)
                    {
                        Parameter prm = (new Parameter(str, rowNum, notifList, query.Trim().StartsWith("INSERT")));
                        if (prm.Name != null)
                            parameterList.Add(prm);
                        rowNum++;
                    }
                    foreach (Parameter prm in parameterList)
                    {
                        String prefix = "";
                        String posfix = "";
                        if (prm.Type.StartsWith("Int"))
                        {
                            prefix = posfix = "";
                        }
                        else if (prm.Type == "Date")
                        {
                            prefix = "TO_DATE ('";
                            if (prm.Value.Length == 10)
                            {
                                posfix = "', 'DD/MM/YYYY')";
                            }
                            else
                            {
                                posfix = "', 'DD/MM/YYYY HH24:MI:SS')";
                            }
                        }
                        else if (prm.Type == "String")
                        {
                            if (prm.Value.StartsWith("\"") && prm.Value.EndsWith("\""))
                                prm.Value = prm.Value.Substring(1, prm.Value.Length - 2);
                            prefix = posfix = "'";

                        }
                        else
                        { prefix = posfix = "'"; }

                        query = query.Replace(":" + prm.Name + " ", prefix + prm.Value + posfix + " ");
                        query = query.Replace(":" + prm.Name + ",", prefix + prm.Value + posfix + ",");
                        query = query.Replace(":" + prm.Name + ")", prefix + prm.Value + posfix + ")");
                    }

                    query = query.Replace("\n", "").Replace("\r", "").Replace(Environment.NewLine, "");

                    ShowDialog(query);

                    if (notifList.Count > 0)
                    {
                         MessageBox.Show(string.Join("\n\r",notifList.ToArray()));
                    }
                }
                else
                {
                    MessageBox.Show(message);
                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(message);
                throw ex;
            }


        }
        #endregion
    }
}