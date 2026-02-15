// Collox.Api, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// Collox.Api.ApiProviderNameAttribute
using System;

[AttributeUsage(AttributeTargets.Class)]
public class ApiProviderNameAttribute : Attribute
{
	public string Name { get; }

	public string Id { get; }

	public ApiProviderNameAttribute(string name, string id)
	{
		Name = name;
		Id = id;
	}
}
