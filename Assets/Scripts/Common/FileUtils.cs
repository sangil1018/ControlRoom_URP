using Common.Extension;
using System.IO;
using System.Text;

using UnityEngine;

namespace Common.Util
{
    public static class FileUtils
    {
        public static StreamReader LoadStreamReader(string filePath)
        {
            try
            {
                if (filePath.IsNullOrEmpty() || File.Exists(filePath) == false)
                {
                    DLogger.Error($"파일을 찾을 수 없습니다: {filePath}");
                    return null;
                }
     
                return new StreamReader(filePath, Encoding.UTF8, true);
            }
            catch (System.Exception e)
            {
                DLogger.TLog($"LoadStreamReader Exception : {e.Message}");
                return null;
            }
        }

        public static StreamReader LoadResourceStreamReader(string filePath)
        {
            try
            {
                if (filePath.IsNullOrEmpty())
                {
                    return null;
                }

                var textAsset = Resources.Load<TextAsset>(Path.ChangeExtension(filePath, ""));
                if (textAsset == null)
                {
                    DLogger.Error($"[{filePath}] 리소스 데이터를 찾을 수 없습니다!");
                    return null;
                }

                var stream = new MemoryStream(textAsset.bytes);                
                return new StreamReader(stream, Encoding.UTF8, true);
            }
            catch (System.Exception e)
            {
                DLogger.TLog($"LoadStreamReader Exception : {e.Message}");
                return null;
            }            
        }      
    }
}
