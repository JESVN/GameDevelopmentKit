//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using Bright.Serialization;
using System.Collections.Generic;
using SimpleJSON;



namespace Game.Hot.Editor
{

public sealed partial class DRArmor :  Bright.Config.EditorBeanBase 
{
    public DRArmor()
    {
    }

    public override void LoadJson(SimpleJSON.JSONObject _json)
    {
        { 
            var _fieldJson = _json["Id"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsNumber) { throw new SerializationException(); }  Id = _fieldJson;
            }
        }
        
        { 
            var _fieldJson = _json["MaxHP"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsNumber) { throw new SerializationException(); }  MaxHP = _fieldJson;
            }
        }
        
        { 
            var _fieldJson = _json["Defense"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsNumber) { throw new SerializationException(); }  Defense = _fieldJson;
            }
        }
        
    }

    public override void SaveJson(SimpleJSON.JSONObject _json)
    {
        {
            _json["Id"] = new JSONNumber(Id);
        }
        {
            _json["MaxHP"] = new JSONNumber(MaxHP);
        }
        {
            _json["Defense"] = new JSONNumber(Defense);
        }
    }

    public static DRArmor LoadJsonDRArmor(SimpleJSON.JSONNode _json)
    {
        DRArmor obj = new DRArmor();
        obj.LoadJson((SimpleJSON.JSONObject)_json);
        return obj;
    }
        
    public static void SaveJsonDRArmor(DRArmor _obj, SimpleJSON.JSONNode _json)
    {
        _obj.SaveJson((SimpleJSON.JSONObject)_json);
    }

    /// <summary>
    /// 装甲编号
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 最大生命
    /// </summary>
    public int MaxHP { get; set; }

    /// <summary>
    /// 防御力
    /// </summary>
    public int Defense { get; set; }

}

}