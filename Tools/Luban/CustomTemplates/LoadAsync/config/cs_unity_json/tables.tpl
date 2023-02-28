using Bright.Serialization;
using System.Threading.Tasks;
using SimpleJSON;
{{
    name = x.name
    namespace = x.namespace
    tables = x.tables
}}

{{cs_start_name_space_grace x.namespace}} 
   
public sealed partial class {{name}}
{
    {{~for table in tables ~}}
{{~if table.comment != '' ~}}
    /// <summary>
    /// {{table.escape_comment}}
    /// </summary>
{{~end~}}
    public {{table.full_name}} {{table.name}} {private set; get; }
    {{~end~}}

    private System.Collections.Generic.Dictionary<string, IDataTable> _tables;

    public System.Collections.Generic.IEnumerable<IDataTable> DataTables => _tables.Values;

    public IDataTable GetDataTable(string tableName) => _tables.TryGetValue(tableName, out var v) ? v : null;

    public async Task LoadAsync(System.Func<string, Task<JSONNode>> loader)
    {
        _tables = new System.Collections.Generic.Dictionary<string, IDataTable>();
        {{~for table in tables ~}}
        {{table.name}} = new {{table.full_name}}(loader("{{table.output_data_file}}"));
        await {{table.name}}.LoadAsync();
        _tables.Add("{{table.full_name}}", {{table.name}});
        {{~end~}}
        PostInit();

        {{~for table in tables ~}}
        {{table.name}}.Resolve(_tables); 
        {{~end~}}
        PostResolve();
    }

    public void TranslateText(System.Func<string, string, string> translator)
    {
        {{~for table in tables ~}}
        {{table.name}}.TranslateText(translator); 
        {{~end~}}
    }
    
    partial void PostInit();
    partial void PostResolve();
}

{{cs_end_name_space_grace x.namespace}}