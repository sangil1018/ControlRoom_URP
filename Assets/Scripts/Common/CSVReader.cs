using UnityEngine;
using UnityEngine.Events;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SimpleJSONUtil;
using System.Reflection;


namespace Common.Util
{
    public class CsvRow : List<string>
    {
        public string LineText { get; set; }
    }

    public struct FieldTypeActionData
    {
        public Type mType;
        public string mFieldName;
        public Action<Type, string, string, JSONNode> mAction;
    }

    public class CsvFileWriter : StreamWriter
    {
        public CsvFileWriter(Stream stream) : base(stream)
        {
        }

        public CsvFileWriter(string filename) : base(filename)
        {
        }

        public void WriteRow(CsvRow row)
        {
            StringBuilder builder = new StringBuilder();
            bool firstColumn = true;
            foreach (string value in row)
            {               
                if (!firstColumn)
                    builder.Append(',');
                if (value.IndexOfAny(new char[] { '"', ',' }) != -1)
                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                else
                    builder.Append(value);
                firstColumn = false;
            }
            row.LineText = builder.ToString();
            WriteLine(row.LineText);
        }

        public void Write<T>(T obj, bool onlyField = false, bool onlyValue = false)
        {
            FieldInfo[] fields = obj.GetType().GetFields();
            CsvRow csvRow = new CsvRow();
            if (!onlyValue)
            {
                foreach (FieldInfo field in fields)
                {
                    if (!field.FieldType.IsClass || field.FieldType.IsArray)
                    {
                        csvRow.Add(field.Name);
                    }
                }
                WriteRow(csvRow);
            }

            if (!onlyField)
            {
                csvRow.Clear();
                foreach (FieldInfo field in fields)
                {
                    if (!field.FieldType.IsClass || field.FieldType.IsArray)
                    {
                        if (field.FieldType.IsArray)
                        {
                            StringBuilder sb = new StringBuilder();
                            Array array = field.GetValue(obj) as Array;
                            if (array != null)
                            {
                                sb.Append("{");
                                bool isFirst = true;
                                foreach (var element in array)
                                {
                                    if (!isFirst)
                                    {
                                        sb.Append(",");
                                    }
                                    sb.Append(element.ToString());
                                    isFirst = false;
                                }
                                sb.Append("}");
                                csvRow.Add(sb.ToString());
                            }
                            else
                            {
                                csvRow.Add(string.Empty);
                            }
                        }
                        else
                        {
                            csvRow.Add(field.GetValue(obj).ToString());
                        }
                    }
                }
                WriteRow(csvRow);
            }
        }

        public void Write<T>(Dictionary<int, T> dic) where T : class
        {
            int i = 0;
            foreach (T obj in dic.Values)
            {
                if (i == 0)
                {
                    Write(obj, false, false);
                }
                else
                {
                    Write(obj, false, true);
                }
                i++;
            }
        }

        public void Write<T>(List<T> list) where T : class
        {
            int i = 0;
            foreach (T obj in list)
            {
                if (i == 0)
                {
                    Write(obj, false, false);
                }
                else
                {
                    Write(obj, false, true);
                }
                i++;
            }
        }
    }

    public static class CSVBuilder
    {
        public static string GetCsvLine(params object[] args)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < args.Length; i++)
            {
                builder.Append(args[i]);

                if (i != args.Length - 1)
                {
                    builder.Append(',');
                }
            }

            return builder.ToString();
        }

        public static string GetVector2(Vector2 value)
        {
            return string.Format("\"({0}, {1})\"", value.x, value.y);
        }

        public static string GetVector2List(List<Vector2> valueList)
        {
            string value = string.Empty;
            for (int i = 0; i < valueList.Count; i++)
            {
                value += CSVBuilder.GetVector2(valueList[i]);

                if (i != valueList.Count - 1)
                {
                    value += ",";
                }
            }

            value = value.Replace("\"", "");
            value = value.Insert(0, "\"{");
            value = value.Insert(value.Length, "}\"");

            return value;
        }

        public static string GetVector3(Vector3 value)
        {
            return string.Format("\"({0}, {1}, {2})\"", value.x, value.y, value.z);
        }

        public static string GetQuaternion(Quaternion value)
        {
            return string.Format("\"({0}, {1}, {2})\"", value.x, value.y, value.z);
        }

        public static string GetColor(Color color)
        {
            return string.Format("\"<{0}, {1}, {2}, {3}>\"", color.r, color.g, color.b, color.a);
        }

        public static string GetString(string value)
        {
            return string.Format("\"{0}\"", value);
        }
    }

    public class CsvFileReader : StreamReader
    {
        public CsvFileReader(Stream stream) : base(stream)
        {
        }

        public CsvFileReader(string filename) : base(filename)
        {
        }

        public bool ReadRow(CsvRow row)
        {
            row.LineText = ReadLine();
            if (String.IsNullOrEmpty(row.LineText))
                return false;

            int pos = 0;
            int rows = 0;

            while (pos < row.LineText.Length)
            {
                string value;

                if (row.LineText[pos] == '"')
                {
                    pos++;

                    int start = pos;
                    while (pos < row.LineText.Length)
                    {
                        if (row.LineText[pos] == '"')
                        {
                            pos++;

                            if (pos >= row.LineText.Length || row.LineText[pos] != '"')
                            {
                                pos--;
                                break;
                            }
                        }
                        pos++;
                    }
                    value = row.LineText.Substring(start, pos - start);
                    value = value.Replace("\"\"", "\"");
                }
                else
                {
                    int start = pos;
                    while (pos < row.LineText.Length && row.LineText[pos] != ',')
                        pos++;
                    value = row.LineText.Substring(start, pos - start);
                }

                if (rows < row.Count)
                    row[rows] = value;
                else
                    row.Add(value);
                rows++;

                while (pos < row.LineText.Length && row.LineText[pos] != ',')
                    pos++;
                if (pos < row.LineText.Length)
                    pos++;
            }

            while (row.Count > rows)
                row.RemoveAt(rows);

            return (row.Count > 0);
        }
    }

    public class CSVReader
    {
        static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
        static char[] TRIM_CHARS = { '\"' };
        static System.Globalization.CultureInfo CULTURE_INFO = System.Globalization.CultureInfo.CurrentCulture;


        public static List<Dictionary<string, object>> Read(string file)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();

            TextAsset data = Resources.Load(file) as TextAsset;
            if (data == null) return list;

            string[] lines = Regex.Split(data.text, LINE_SPLIT_RE);
            if (lines.Length <= 1) return list;

            string[] header = Regex.Split(lines[0], SPLIT_RE);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = Regex.Split(lines[i], SPLIT_RE);
                if (values.Length == 0 || values[0] == "") continue;

                Dictionary<string, object> entry = new Dictionary<string, object>();
                for (int j = 0; j < header.Length && j < values.Length; j++)
                {
                    string value = values[j];
                    value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");

                    object finalvalue = value;
                    int n; float f; double d; bool b; byte c;
                    if (int.TryParse(value, out n)) finalvalue = n;
                    else if (bool.TryParse(value, out b)) finalvalue = b;
                    else if (float.TryParse(value, System.Globalization.NumberStyles.Float, CULTURE_INFO, out f)) finalvalue = f;
                    else if (double.TryParse(value, System.Globalization.NumberStyles.Float, CULTURE_INFO, out d)) finalvalue = d;
                    else if (byte.TryParse(value, out c)) finalvalue = c;

                    entry[header[j]] = finalvalue;
                }
                list.Add(entry);
            }
            return list;
        }               

        public static class CsvReadHelper
        {
            public static List<FieldTypeActionData> GetFieldTypeList(Type targetType, List<string> headerList)
            {
                List<FieldTypeActionData> result = new List<FieldTypeActionData>();
                FieldTypeActionData data;

                System.Reflection.FieldInfo[] fieldInfoList = targetType.GetFields();
                List<string> memberFieldNames = new List<string>();
                List<Type> memberFieldTypes = new List<Type>();

                for (int i = 0; i < fieldInfoList.Length; i++)
                {
                    memberFieldNames.Add(fieldInfoList[i].Name);
                    memberFieldTypes.Add(fieldInfoList[i].FieldType);
                }

                for (int i = 0; i < headerList.Count; i++)
                {
                    int index = memberFieldNames.FindIndex(c => c.ToLower() == headerList[i].Trim().ToLower());
                    bool checkField = index != -1;

                    if (checkField == true)
                    {
                        data = new FieldTypeActionData();
                        data.mType = memberFieldTypes[index];
                        data.mFieldName = memberFieldNames[index];
                        AddTypeAction(data.mType, ref data.mAction);
                        result.Add(data);
                    }
                    else
                    {
                        data = new FieldTypeActionData();
                        result.Add(data);
                    }
                }

                return result;
            }

            public static void AddJsonNode(Type fieldType, string key, string value, JSONNode node)
            {
                if (fieldType == typeof(int))
                {
                    SetInt(fieldType, key, value, node);
                }
                else if(fieldType == typeof(long))
                {
                    SetLong(fieldType, key, value, node);
                }
                else if (fieldType == typeof(string))
                {
                    SetString(fieldType, key, value, node);
                }
                else if (fieldType == typeof(float))
                {
                    SetFloat(fieldType, key, value, node);
                }
                else if (fieldType.IsArray)
                {
                    if (fieldType == typeof(Vector2[]))
                    {
                        SetVector2Array(fieldType, key, value, node);
                    }
                    else if(fieldType == typeof(Vector3[]))
                    {
                        SetVector3Array(fieldType, key, value, node);
                    }
                    else if(fieldType == typeof(Vector4[]))
                    {
                        SetVector4Array(fieldType, key, value, node);
                    }
                    else if (fieldType == typeof(int[]))
                    {
                        SetIntArray(fieldType, key, value, node);
                    }
                    else if (fieldType == typeof(float[]))
                    {
                        SetFloatArray(fieldType, key, value, node);
                    }
                    else if (fieldType.GetElementType() != null && fieldType.GetElementType().IsEnum)
                    {
                        SetEnumArray(fieldType, key, value, node);
                    }
                    else
                    {
                        SetDefaultArray(fieldType, key, value, node);
                    }
                }
                else if (fieldType.IsEnum)
                {
                    SetEnum(fieldType, key, value, node);
                }
                else if (fieldType == typeof(Vector3) || fieldType == typeof(Vector2) || fieldType == typeof(Vector4))
                {
                    SetVector(fieldType, key, value, node);
                }
                else if (fieldType == typeof(Color) || fieldType == typeof(Color32))
                {
                    SetColor(fieldType, key, value, node);
                }
                else if (fieldType == typeof(double))
                {
                    SetDouble(fieldType, key, value, node);
                }
                else if (fieldType == typeof(bool))
                {
                    SetBool(fieldType, key, value, node);
                }
                else if (fieldType == typeof(byte))
                {
                    SetByte(fieldType, key, value, node);
                }
                else if (fieldType == typeof(short))
                {
                    SetShort(fieldType, key, value, node);
                }
                else if (fieldType == typeof(ushort))
                {
                    SetUshort(fieldType, key, value, node);
                }
                else
                {
                    DLogger.Error($"정의되지 않은 타입입니다. 타입:{fieldType.ToString()}");
                }
            }

            public static void AddTypeAction(Type fieldType, ref Action<Type, string, string, JSONNode> actionList)
            {
                if (fieldType == typeof(int))
                {
                    actionList = SetInt;
                }
                else if (fieldType == typeof(long))
                {
                    actionList = SetLong;
                }
                else if (fieldType == typeof(string))
                {
                    actionList = SetString;
                }
                else if (fieldType == typeof(float))
                {
                    actionList = SetFloat;
                }               
                else if (fieldType.IsArray)
                {
                    if (fieldType == typeof(Vector2[]))
                    {
                        actionList = SetVector2Array;
                    }
                    else if(fieldType == typeof(Vector3[]))
                    {
                        actionList = SetVector3Array;
                    }
                    else if(fieldType == typeof(Vector4[]))
                    {
                        actionList = SetVector4Array;
                    }
                    else if (fieldType == typeof(int[]))
                    {
                        actionList = SetIntArray;
                    }
                    else if (fieldType == typeof(float[]))
                    {
                        actionList = SetFloatArray;
                    }
                    else if (fieldType.GetElementType() != null && fieldType.GetElementType().IsEnum)
                    {
                        actionList = SetEnumArray;
                    }
                    else
                    {
                        actionList = SetDefaultArray;
                    }
                }
                else if (fieldType.IsEnum)
                {
                    actionList = SetEnum;
                }
                else if (fieldType == typeof(Vector3) || fieldType == typeof(Vector2))
                {
                    actionList = SetVector;
                }
                else if (fieldType == typeof(Color) || fieldType == typeof(Color32))
                {
                    actionList = SetColor;
                }
                else if (fieldType == typeof(double))
                {
                    actionList = SetDouble;
                }
                else if (fieldType == typeof(bool))
                {
                    actionList = SetBool;
                }
                else if (fieldType == typeof(byte))
                {
                    actionList = SetByte;
                }
                else if (fieldType == typeof(short))
                {
                    actionList = SetShort;
                }
                else if (fieldType == typeof(ushort))
                {
                    actionList = SetUshort;
                }
                else
                {
                    actionList = null;
                    DLogger.Error("정의되지 않은 타입입니다.");
                }
            }

            public static List<string> GetColumn(string line)
            {
                List<string> column = new List<string>();
                char splitArray = '\"';
                char splitComma = ',';
                char splitBar = '|';

                if (line.Contains("\"") == false)
                {
                    return new List<string>(line.Split(','));
                }
                else
                {
                    int startCommaIndex = 0;
                    int startArrayIndex = -1;
                    bool checkArray = false;

                    for (int i = 0; i < line.Length; i++)
                    {
                        if (line[i] == splitArray)
                        {
                            // check array
                            if (startArrayIndex == -1)
                            {
                                checkArray = true;
                                startArrayIndex = i;
                            }
                            else
                            {
                                checkArray = false;
                            }
                        }
                        else if (line[i] == splitBar)
                        {
                            startCommaIndex = i + 1;
                            break;
                        }
                        else if (checkArray == false && line[i] == splitComma)
                        {
                            // add array
                            if (startArrayIndex != -1)
                            {
                                column.Add(line.Substring(startArrayIndex, i - startArrayIndex));
                                startArrayIndex = -1;
                                startCommaIndex = i + 1;

                                continue;
                            }

                            // add column
                            column.Add(line.Substring(startCommaIndex, i - startCommaIndex));
                            startCommaIndex = i + 1;
                        }
                    }

                    // end column
                    column.Add(line.Substring(startCommaIndex));
                }

                return column;
            }


            private static void SetEnum(Type fieldType, string key, string value, JSONNode node)
            {
                string enumValue = value.Trim();
                if (Enum.IsDefined(fieldType, enumValue) == true)
                {
                    node[key].AsInt = (int)Enum.Parse(fieldType, enumValue);
                }
            }

            private static void SetString(Type fieldType, string key, string value, JSONNode node)
            {
                node[key] = value;
            }

            private static void SetInt(Type fieldType, string key, string value, JSONNode node)
            {
                int result = 0;
                int.TryParse(value, out result);
                node[key].AsInt = result;
            }

            private static void SetFloat(Type fieldType, string key, string value, JSONNode node)
            {
                float result = 0.0f;
                float.TryParse(value, System.Globalization.NumberStyles.Float, CULTURE_INFO, out result);
                node[key].AsFloat = result;
            }

            private static void SetLong(Type fieldType, string key, string value, JSONNode node)
            {
                long result = 0;
                long.TryParse(value, out result);
                node[key].AsLong = result;
            }

            private static void SetBool(Type fieldType, string key, string value, JSONNode node)
            {
                bool result = false;
                bool.TryParse(value, out result);
                node[key].AsBool = result;
            }

            private static void SetDouble(Type fieldType, string key, string value, JSONNode node)
            {
                double result = 0.0f;
                double.TryParse(value, System.Globalization.NumberStyles.Float, CULTURE_INFO, out result);
                node[key].AsDouble = result;
            }

            private static void SetByte(Type fieldType, string key, string value, JSONNode node)
            {
                byte result = 0;
                byte.TryParse(value, out result);
                node[key].AsByte = result;
            }

            private static void SetUshort(Type fieldType, string key, string value, JSONNode node)
            {
                ushort result = 0;
                ushort.TryParse(value, out result);
                node[key].AsUshort = result;
            }

            private static void SetShort(Type fieldType, string key, string value, JSONNode node)
            {
                short result = 0;
                short.TryParse(value, out result);
                node[key].AsShort = result;
            }

            private static void SetVector(Type fieldType, string key, string value, JSONNode node)
            {
                string[] elements = value.TrimStart('(').TrimEnd(')').Split(',');
                switch (elements.Length)
                {
                    case 2:
                        node[key].AsJson = new Vector2(ToFloat(elements[0]), ToFloat(elements[1]));
                        break;
                    case 3:
                        node[key].AsJson = new Vector3(ToFloat(elements[0]), ToFloat(elements[1]), ToFloat(elements[2]));
                        break;
                    case 4:
                        node[key].AsJson = new Vector4(ToFloat(elements[0]), ToFloat(elements[1]), ToFloat(elements[2]), ToFloat(elements[3]));
                        break;
                }
            }

            private static void SetColor(Type fieldType, string key, string value, JSONNode node)
            {
                float f;
                byte c;

                string[] elements = value.Trim('<').Trim('>').Split(',');

                if (elements.Length == 4)
                {
                    if (float.TryParse(elements[0], out f))
                    {
                        node[key].AsJson = new Color(ToFloat(elements[0]), ToFloat(elements[1]), ToFloat(elements[2]), ToFloat(elements[3]));
                    }
                    else if (byte.TryParse(elements[0], out c))
                    {
                        node[key].AsJson = new Color32(ToByte(elements[0]), ToByte(elements[1]), ToByte(elements[2]), ToByte(elements[3]));
                    }
                }
            }

            private static void SetIntArray(Type fieldType, string key, string value, JSONNode node)
            {
                string trimValue = value.TrimStart('{').TrimEnd('}');

                if (false == string.IsNullOrEmpty(trimValue))
                {
                    string[] elements = trimValue.Split(',');
                    if (elements.Length > 0)
                    {
                        int n;
                        int[] arr = new int[elements.Length];
                        for (int k = 0; k < elements.Length; ++k)
                        {
                            if (int.TryParse(elements[k], out n))
                                arr[k] = n;
                        }
                        node[key].AsJson = new IntArray(arr);

                        int startIndex = node[key].Value.IndexOf('[');
                        int endIndex = node[key].Value.IndexOf(']') - startIndex;
                        node[key].Value = node[key].Value.Substring(startIndex, endIndex + 1);
                    }
                }
            }

            private static void SetFloatArray(Type fieldType, string key, string value, JSONNode node)
            {
                string trimValue = value.TrimStart('{').TrimEnd('}');

                if (false == string.IsNullOrEmpty(trimValue))
                {
                    string[] elements = trimValue.Split(',');
                    if (elements.Length > 0)
                    {
                        float f;
                        float[] arr = new float[elements.Length];
                        for (int k = 0; k < elements.Length; ++k)
                        {
                            if (float.TryParse(elements[k], out f))
                                arr[k] = f;
                        }
                        node[key].AsJson = new FloatArray(arr);

                        int startIndex = node[key].Value.IndexOf('[');
                        int endIndex = node[key].Value.IndexOf(']') - startIndex;
                        node[key].Value = node[key].Value.Substring(startIndex, endIndex + 1);
                    }
                }
            }

            private static void SetEnumArray(Type fieldType, string key, string value, JSONNode node)
            {
                string trimValue = value.TrimStart('{').TrimEnd('}');

                if (false == string.IsNullOrEmpty(trimValue))
                {
                    string[] elements = trimValue.Split(',');
                    if (elements.Length > 0)
                    {
                        int[] arr = new int[elements.Length];
                        string elementValue;
                        for (int k = 0; k < elements.Length; ++k)
                        {
                            elementValue = elements[k].Trim();
                            if (Enum.IsDefined(fieldType.GetElementType(), elementValue) == true)
                                arr[k] = (int)Enum.Parse(fieldType.GetElementType(), elementValue);
                        }
                        node[key].AsJson = new IntArray(arr);

                        int startIndex = node[key].Value.IndexOf('[');
                        int endIndex = node[key].Value.IndexOf(']') - startIndex;
                        node[key].Value = node[key].Value.Substring(startIndex, endIndex + 1);
                    }
                }                                
            }

            private static void SetDefaultArray(Type fieldType, string key, string value, JSONNode node)
            {
                string trimValue = value.TrimStart('{').TrimEnd('}');

                if (false == string.IsNullOrEmpty(trimValue))
                {
                    string[] elements = trimValue.Split(',');
                    if (elements.Length > 0)
                    {
                        node[key].AsJson = new StringArray(elements);

                        int startIndex = node[key].Value.IndexOf('[');
                        int endIndex = node[key].Value.IndexOf(']') - startIndex;
                        node[key].Value = node[key].Value.Substring(startIndex, endIndex + 1);
                    }
                }
            }

            private static void SetVector2Array(Type fieldType, string key, string value, JSONNode node)
            {
                Vector2[] vectorList = null;

                string[] elements = value.Replace("(", "").Replace(")", "").TrimStart('{').TrimEnd('}').Split(',');
                int length = elements.Length / 2;
                if (length > 0)
                {
                    vectorList = new Vector2[length];

                    int vecCount = 0;
                    for (int i = 0; i < elements.Length; i += 2)
                    {
                        vectorList[vecCount].x = ToFloat(elements[i]);
                        vectorList[vecCount].y = ToFloat(elements[i + 1]);

                        ++vecCount;
                    }
                }

                node[key].AsJson = new Vector2Array(vectorList);

                int startIndex = node[key].Value.IndexOf('[');
                int endIndex = node[key].Value.IndexOf(']') - startIndex;
                node[key].Value = node[key].Value.Substring(startIndex, endIndex + 1);
            }

        private static void SetVector3Array(Type fieldType, string key, string value, JSONNode node)
        {
            Vector3[] vectorList = null;

            string[] elements = value.Replace("(", "").Replace(")", "").TrimStart('{').TrimEnd('}').Split(',');
            int length = elements.Length / 3;
            if (length > 0)
            {
                vectorList = new Vector3[length];

                int vecCount = 0;
                for (int i = 0; i < elements.Length; i += 3)
                {
                    vectorList[vecCount].x = ToFloat(elements[i]);
                    vectorList[vecCount].y = ToFloat(elements[i + 1]);
                    vectorList[vecCount].z = ToFloat(elements[i + 2]);
                    ++vecCount;
                }
            }

            node[key].AsJson = new Vector3Array(vectorList);

            int startIndex = node[key].Value.IndexOf('[');
            int endIndex = node[key].Value.IndexOf(']') - startIndex;
            node[key].Value = node[key].Value.Substring(startIndex, endIndex + 1);
        }

        private static void SetVector4Array(Type fieldType, string key, string value, JSONNode node)
        {
            Vector4[] vectorList = null;

            string[] elements = value.Replace("(", "").Replace(")", "").TrimStart('{').TrimEnd('}').Split(',');
            int length = elements.Length / 4;
            if (length > 0)
            {
                vectorList = new Vector4[length];

                int vecCount = 0;
                for (int i = 0; i < elements.Length; i += 4)
                {
                    vectorList[vecCount].x = ToFloat(elements[i]);
                    vectorList[vecCount].y = ToFloat(elements[i + 1]);
                    vectorList[vecCount].z = ToFloat(elements[i + 2]);
                    vectorList[vecCount].w = ToFloat(elements[i + 3]);
                    ++vecCount;
                }
            }

            node[key].AsJson = new Vector4Array(vectorList);

            int startIndex = node[key].Value.IndexOf('[');
            int endIndex = node[key].Value.IndexOf(']') - startIndex;
            node[key].Value = node[key].Value.Substring(startIndex, endIndex + 1);
        }
    }

    public static Dictionary<int, T> Read<T>(string file)
    {
        StreamReader streamReader = FileUtils.LoadStreamReader(file);
        if (streamReader == null)
        {
            DLogger.Error($"CSVUtil.Read() : [{file}] file read error!");
            return null;
        }

        Dictionary<int, T> dictionary = new Dictionary<int, T>();

        string headLine = streamReader.ReadLine();

        List<string> headerList = CsvReadHelper.GetColumn(headLine);
        List<string> values;

        List<FieldTypeActionData> fieldTypeList = CsvReadHelper.GetFieldTypeList(typeof(T), headerList);

        int headerCount = headerList.Count;
        string line = string.Empty;

        while ((line = streamReader.ReadLine()) != null)
        {
            string key, value;

            JSONNode js = JSON.Parse("{}");

            values = CsvReadHelper.GetColumn(line);

            int keyIndex = 0;
            int valueCount = values.Count;

            if (values.Count == headerCount)
            {
                for (int i = 0; i < fieldTypeList.Count; i++)
                {
                    if (fieldTypeList[i].mAction == null)
                        continue;

                    if (i >= values.Count)
                    {
                        Debug.LogError(file + " /// " + "Error : " + headerCount + " /// " + values.Count);
                        break;
                    }

                    key = fieldTypeList[i].mFieldName;
                    value = values[i];

                    value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\n", "\n").Replace("\\", "");

                    fieldTypeList[i].mAction(fieldTypeList[i].mType, key, value, js);

                    if (i == 0)
                    {
                        keyIndex = js[key].AsInt;
                    }
                }

                value = js.ToString();
                dictionary[keyIndex] = JsonUtility.FromJson<T>(value);
            }
            else
            {
                //DLogger.Error(file + " /// " + "Error : " + headerCount + " /// " + values.Count);
            }
        }
        streamReader.Close();
        fieldTypeList.Clear();

        fieldTypeList = null;
        return dictionary;
    }   

    public static Dictionary<int, T> ReadFromResource<T>(string file)
    {
        string fileExtension = Path.GetExtension(file);
        if (false == string.IsNullOrEmpty(fileExtension))
        {
            file = file.Replace(fileExtension, "");
        }

        TextAsset data = Resources.Load(file) as TextAsset;
        if (data == null) return null;
        string[] lines = Regex.Split(data.text, LINE_SPLIT_RE);
        int lineCount = lines.Length;
        if (lineCount <= 1) return null;

        Dictionary<int, T> dictionary = new Dictionary<int, T>();

        string headLine = lines[0];

        List<string> header = CsvReadHelper.GetColumn(headLine);
        List<string> values;

        List<FieldTypeActionData> fieldTypeList = CsvReadHelper.GetFieldTypeList(typeof(T), header);
        int headerCount = header.Count;

            for (int i = 1; i < lines.Length; i++)
            {
                string key, value;

                JSONNode js = JSON.Parse("{}");

                values = CsvReadHelper.GetColumn(lines[i]);

                int keyIndex = 0;
                int valueCount = values.Count;

                if (values.Count >= headerCount)
                {
                    for (int j = 0; j < headerCount; j++)
                    {
                        if (fieldTypeList[j].mAction == null)
                        {
                            continue;
                        }

                        key = header[j];
                        value = values[j];

                        value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\n", "\n").Replace("\\", "");

                        fieldTypeList[j].mAction(fieldTypeList[j].mType, key, value, js);

                        if (j == 0)
                        {
                            keyIndex = js[key].AsInt;
                            if (keyIndex == 0)
                            {
                                Debug.Log("Invalid TableIndex(0)!!! : " + file);
                                return dictionary;
                            }
                        }
                    }

                    value = js.ToString();
                    dictionary[keyIndex] = JsonUtility.FromJson<T>(value);
                }
                else
                {
                    //DLogger.Error(file + " /// " + "Error : " + headerCount + " /// " + values.Count);
                }
            }

        fieldTypeList.Clear();

        fieldTypeList = null;
        return dictionary;
    }

    public static string[] ReadCSV(string file)
    {
        StreamReader streamReader = null;
        try
        {
            streamReader = new StreamReader(file, System.Text.Encoding.UTF8, true);
        }
        catch (Exception e)
        {
            Debug.Log(e + " : File not exist!!!");
            return null;
        }
        if (streamReader == null)
        {
            Debug.LogErrorFormat("CSVUtil.Read() : {0} file read error!", file);
            return null;
        }
        string streamText = streamReader.ReadToEnd();
        streamReader.Close();

        string[] lines = Regex.Split(streamText, LINE_SPLIT_RE);

        return lines;
    }

    public static float ToFloat(string s)
    {
        float f;
        return float.TryParse(s, out f) ? f : 0.0f;
    }

    public static byte ToByte(string s)
    {
        byte c;
        return byte.TryParse(s, out c) ? c : (byte)0;
    }
                
        public static List<string> GetColumn(string line)
        {
            List<string> column = new List<string>();
            char splitArray = '\"';
            char splitComma = ',';

            if (line.Contains("\"") == false)
            {
                return new List<string>(line.Split(','));
            }
            else
            {
                int startCommaIndex = 0;
                int startArrayIndex = 0;
                bool checkArray = false;

                for (int i = 0; i < line.Length; i++)
                {
                    if (line[i] == splitArray)
                    {
                        // check array
                        if (startArrayIndex == 0)
                        {
                            checkArray = true;
                            startArrayIndex = i;
                        }
                        else
                        {
                            checkArray = false;
                        }
                    }
                    else if (checkArray == false && line[i] == splitComma)
                    {
                        // add array
                        if (startArrayIndex != 0)
                        {
                            column.Add(line.Substring(startArrayIndex, i - startArrayIndex));
                            startArrayIndex = 0;
                            startCommaIndex = i + 1;

                            continue;
                        }

                        // add column
                        column.Add(line.Substring(startCommaIndex, i - startCommaIndex));
                        startCommaIndex = i + 1;
                    }
                }

                // end column
                column.Add(line.Substring(startCommaIndex));
            }

            return column;
        }
    }
}