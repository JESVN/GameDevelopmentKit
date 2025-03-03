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



namespace ET.Editor
{

public sealed partial class DRDemo :  Bright.Config.EditorBeanBase 
{
    public DRDemo()
    {
            Name = new Bright.Config.EditorText("", "");
            TestMap = new System.Collections.Generic.Dictionary<string,string>();
    }

    public override void LoadJson(SimpleJSON.JSONObject _json)
    {
        { 
            var _fieldJson = _json["id"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsNumber) { throw new SerializationException(); }  Id = _fieldJson;
            }
        }
        
        { 
            var _fieldJson = _json["name"];
            if (_fieldJson != null)
            {
                Name = Bright.Config.EditorText.LoadJson(_fieldJson);
            }
        }
        
        { 
            var _fieldJson = _json["testMap"];
            if (_fieldJson != null)
            {
                if(!_fieldJson.IsArray) { throw new SerializationException(); } TestMap = new System.Collections.Generic.Dictionary<string, string>(); foreach(JSONNode __e in _fieldJson.Children) { string __k;  if(!__e[0].IsString) { throw new SerializationException(); }  __k = __e[0]; string __v;  if(!__e[1].IsString) { throw new SerializationException(); }  __v = __e[1];  TestMap.Add(__k, __v); }  
            }
        }
        
    }

    public override void SaveJson(SimpleJSON.JSONObject _json)
    {
        {
            _json["id"] = new JSONNumber(Id);
        }
        {

            if (Name == null) { throw new System.ArgumentNullException(); }
            _json["name"] = Bright.Config.EditorText.SaveJson(Name);
        }
        {

            if (TestMap == null) { throw new System.ArgumentNullException(); }
            { var __cjson = new JSONArray(); foreach(var _e in TestMap) { var __entry = new JSONArray(); __cjson[null] = __entry; __entry["null"] = new JSONString(_e.Key); __entry["null"] = new JSONString(_e.Value); } _json["testMap"] = __cjson; }
        }
    }

    public static DRDemo LoadJsonDRDemo(SimpleJSON.JSONNode _json)
    {
        DRDemo obj = new DRDemo();
        obj.LoadJson((SimpleJSON.JSONObject)_json);
        return obj;
    }
        
    public static void SaveJsonDRDemo(DRDemo _obj, SimpleJSON.JSONNode _json)
    {
        _obj.SaveJson((SimpleJSON.JSONObject)_json);
    }

    public int Id { get; set; }

    /// <summary>
    /// key
    /// </summary>
    public Bright.Config.EditorText Name { get; set; }

    public System.Collections.Generic.Dictionary<string, string> TestMap { get; set; }

}

}