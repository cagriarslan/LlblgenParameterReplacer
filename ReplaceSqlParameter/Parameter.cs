using System;
using System.Collections.Generic;
using System.Text;

namespace ReplaceSqlParameter
{
    class Parameter
    {
        String name;

        public String Name
        {
            get { return name; }
            set { name = value; }
        }
        String type;

        public String Type
        {
            get { return type; }
            set { type = value; }
        }
        String value;

        public String Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        public Parameter(String all, int stariNo, List<string> notifList,bool insertStat = false)
        {

            try
            {
                if (all.Contains("Parameter: :"))
                {
                    all = all.Trim();
                    all = all.Replace("Parameter: :", "");
                    name = all.Substring(0, all.IndexOf(":")).Trim();
                    all = all.Replace(name, "").Trim();
                    type = all.Substring(1, all.IndexOf(".") - 1).Trim();
                    value = "1";
                    value = all.Substring(all.LastIndexOf("Value:") + 6);
                    value = value.Substring(0, value.Length - 1).Trim();
                    if (insertStat && value == "<undefined value>")
                    {
                        value = "null";
                    }
                }
                else if (string.IsNullOrEmpty(all))
                {
                    name = null;
                }
                else
                {
                    notifList.Add(stariNo + ". parametre parse edilemedi:"+all);
                }
            }
            catch (Exception)
            {
                if (!notifList.Contains(stariNo + ". parametre parse edilemedi"))
                    notifList.Add(stariNo + ". parametre parse edilemedi:"+all);
            }
        }

    }
}
