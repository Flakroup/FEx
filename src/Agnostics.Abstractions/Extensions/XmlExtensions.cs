using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace FEx.Agnostics.Abstractions.Extensions;

public static class XmlExtensions
{
    public static T Deserialize<T>(this XmlSerializer serializer, Stream stream) where T : class =>
        (T)serializer.Deserialize(stream);

    public static string Serialize<T>(this XmlSerializer serializer, T value) where T : class
    {
        using var memStr = new MemoryStream();
        memStr.Position = 0;
        serializer.Serialize(memStr, value);

        return Convert.ToBase64String(memStr.ToArray());
    }

    public static XmlSchema Add(this XmlSchemaSet xmlSchemaSet, string targetNamespace, Stream schemaStream)
    {
        using (var schemaReader = XmlReader.Create(schemaStream))
            xmlSchemaSet.Add(targetNamespace, schemaReader);

        XmlSchema lastSchema = null;

        foreach (object schema in xmlSchemaSet.Schemas(targetNamespace))
            lastSchema = schema as XmlSchema;

        return lastSchema;
    }

    public static XmlSchema AddManifestResourceSchema(this XmlSchemaSet xmlSchemaSet,
                                                      Assembly resourceAssembly,
                                                      string targetNamespace,
                                                      string name)
    {
        using Stream schemaStream = resourceAssembly.GetManifestResourceStream(name);

        return xmlSchemaSet.Add(targetNamespace, schemaStream);
    }

    public static T ValidateAndDeserialize<T>(this XmlSerializer serializer, string xml, Func<XmlSchemaSet> func)
        where T : class
    {
        byte[] data = Encoding.ASCII.GetBytes(xml);
        using var stream = new MemoryStream(data, 0, data.Length);

        return ValidateAndDeserialize<T>(serializer, stream, func);
    }

    public static T ValidateAndDeserialize<T>(this XmlSerializer serializer, Stream stream, Func<XmlSchemaSet> func)
        where T : class
    {
        Validate(stream, func);

        return serializer.Deserialize<T>(stream);
    }

    public static string SerializeAndValidate<T>(this XmlSerializer serializer, T obj, Func<XmlSchemaSet> func)
        where T : class
    {
        string xml = serializer.Serialize(obj);
        byte[] data = Encoding.ASCII.GetBytes(xml);
        using var stream = new MemoryStream(data, 0, data.Length);
        Validate(stream, func);

        return xml;
    }

    private static void Validate(Stream stream, Func<XmlSchemaSet> func)
    {
        var xmlReaderSettings = new XmlReaderSettings();
        xmlReaderSettings.Schemas.Add(func?.Invoke());
        xmlReaderSettings.ValidationType = ValidationType.Schema;

        string warningAndErrorsText = string.Empty;
        var containsError = false;

        xmlReaderSettings.ValidationEventHandler += (_, e) =>
        {
            switch (e.Severity)
            {
                case XmlSeverityType.Warning:
                    warningAndErrorsText += $"WARNING: {e.Message}\n";

                    break;
                case XmlSeverityType.Error:
                    containsError = true;
                    warningAndErrorsText += $"ERROR: {e.Message}\n";

                    break;
            }
        };

        using (var requestXml = XmlReader.Create(stream, xmlReaderSettings))
        {
            while (requestXml.Read())
            {
            }
        }

        if (containsError)
            throw new XmlSchemaException(warningAndErrorsText);
    }
}