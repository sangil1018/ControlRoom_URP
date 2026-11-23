using UnityEngine;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Common;
using Common.Util;


public class TableManager : Singleton2<TableManager>
{   
    private Dictionary<Type, IDictionary> mTables = new ();


    public void LoadTables()
    {
        UnloadTables();

        LoadTable<StringTableData>("string_table.csv");
        LoadTable<SoundTableData>("sound_table.csv");
        LoadTable<NarrationTableData>("narration_table.csv");
    }

    public void UnloadTables()
    {
        if (mTables.Any())
        {
            foreach (KeyValuePair<Type, IDictionary> pair in mTables)
            {
                pair.Value.Clear();
            }
            mTables.Clear();
        }        
    }
    
    private bool LoadTable<T>(string fileName)
    {
        string fullPath = Path.Combine("tables", fileName);

        var loadTable = CSVReader.ReadFromResource<T>(fullPath);
        if (loadTable == null)
        {
            DLogger.Error($"[{fullPath}] 테이블을 찾을수 없습니다! ");
            return false;
        }

        var tableKey = typeof(T);
        if (mTables.ContainsKey(tableKey))
        {
            var hasTable = mTables[tableKey];
            var enumerator = loadTable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var keyPair = enumerator.Current;
                if (hasTable.Contains(keyPair.Key) == false)
                    hasTable.Add(keyPair.Key, keyPair.Value);
                else
                    DLogger.Error($"***테이블 중복 데이터 예외처리: 테이블[{tableKey}] 키[{keyPair.Key}]");
            }
        }
        else
        {
            mTables.Add(tableKey, loadTable);
        }

        DLogger.Log($"[{fullPath}] 테이블 로딩 완료...");
        return true;
    }

    public Dictionary<int, T> GetTable<T>()
    {
        if (mTables.ContainsKey(typeof(T)) == false)
        {
            return null;
        }
        return mTables[typeof(T)] as Dictionary<int, T>;
    }

    public T GetData<T>(int index) where T : class
    {
        var dataTable = GetTable<T>();
        if (dataTable == null || dataTable.ContainsKey(index) == false)
        {            
            return null;
        }
        return dataTable[index];
    }

    public static string GetString(int index)
    {
        if (IsInstance())
        {
            var stringData = Instance.GetData<StringTableData>(index);
            if (stringData != null)
                return stringData.text;
        }

        DLogger.Error($"[{index}] 스트링 테이블 데이터를 찾을 수 없습니다!");
        return string.Empty;
    }
}
