# Database Context for Stored Procedure - .net 4.6.1
Data provider from stored procedure as abstract level of database 

![](https://sqlmentalist.files.wordpress.com/2011/08/image.png)

## Using
Sample model
```
public class SampleStoredProcedureName
{
    public int Param1 { set; get; }
    public int Param2 { set; get; }
    public int Param3 { set; get; }
}

public class SampleResultModel
{
    public int Test1 { set; get; }
    public int Test2 { set; get; }
    public int? Test3 { set; get; }
    public int Test4 { set; get; }
}
```

### Model as Stored Procedure name and properties as parameters:

ExecuteToList
```
using (var db = new ContextDb(Config.ConnectionStrings.MASTER))
{
    db.SetCommandText(new SampleStoredProcedureName(){ Param1 = 1, Param2 = 2, Param3 = 4});
    var result = db.ExecuteToList<SampleResultModel>();
}
```

ExecuteFirstOrDefault
```
using (var db = new ContextDb(Config.ConnectionStrings.MASTER))
{
    db.SetCommandText(new SampleStoredProcedureName(){ Param1 = 1, Param2 = 2, Param3 = 4});
    var result = db.db.ExecuteFirstOrDefault<SampleResultModel>();
}
```

### Stored Procedure as String:

ExecuteFirstOrDefault
```
using (var db = new ContextDb(Config.ConnectionStrings.MASTER))
{
    var result = db.ExecuteFirstOrDefault<SampleResultModel>("SampleStoredProcedureName");
}
```
