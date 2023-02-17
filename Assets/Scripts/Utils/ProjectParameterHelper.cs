using System;
using Newtonsoft.Json;

public static class ProjectParameterHelper
{
    public static object GetValue(IO.Swagger.Model.ProjectParameter parameter) {
        switch (parameter.Type) {
           case "integer":
                return JsonConvert.DeserializeObject<int>(parameter.Value);
            case "string":
                return JsonConvert.DeserializeObject<string>(parameter.Value);
            case "double":
                return JsonConvert.DeserializeObject<double>(parameter.Value);
            case "boolean":
                return JsonConvert.DeserializeObject<bool>(parameter.Value);
            default:
                throw new Base.RequestFailedException($"Unsupported parameter type {parameter.Type}");
        }
    }
}
