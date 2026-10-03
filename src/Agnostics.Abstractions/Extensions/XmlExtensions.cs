using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace FEx.Agnostics.Abstractions.Extensions;

/// <summary>Extensions for serializing, deserializing and schema-validating XML.</summary>
public static class XmlExtensions
{
    /// <summary>Deserializes an object from a stream.</summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="serializer">The serializer.</param>
    /// <param name="stream">The stream holding the XML.</param>
    /// <returns>The deserialized object.</returns>
    public static T? Deserialize<T>(this XmlSerializer serializer, Stream stream) where T : class =>
        (T?)serializer.Deserialize(stream);

    /// <summary>Serializes an object to XML and returns the XML bytes as Base64 text.</summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="serializer">The serializer.</param>
    /// <param name="value">The object to serialize.</param>
    /// <returns>The Base64-encoded XML.</returns>
    public static string Serialize<T>(this XmlSerializer serializer, T value) where T : class
    {
        using var memStr = new MemoryStream();
        memStr.Position = 0;
        serializer.Serialize(memStr, value);

        return Convert.ToBase64String(memStr.ToArray());
    }

    /// <summary>Adds a schema read from a stream to a schema set.</summary>
    /// <param name="xmlSchemaSet">The schema set to add to.</param>
    /// <param name="targetNamespace">The target namespace of the schema.</param>
    /// <param name="schemaStream">The stream holding the schema.</param>
    /// <returns>The last schema registered for the namespace.</returns>
    public static XmlSchema? Add(this XmlSchemaSet xmlSchemaSet, string targetNamespace, Stream schemaStream)
    {
        using (var schemaReader = XmlReader.Create(schemaStream))
            xmlSchemaSet.Add(targetNamespace, schemaReader);

        XmlSchema? lastSchema = null;

        foreach (var schema in xmlSchemaSet.Schemas(targetNamespace))
            lastSchema = schema as XmlSchema;

        return lastSchema;
    }

    /// <summary>Adds a schema embedded as a manifest resource to a schema set.</summary>
    /// <param name="xmlSchemaSet">The schema set to add to.</param>
    /// <param name="resourceAssembly">The assembly containing the resource.</param>
    /// <param name="targetNamespace">The target namespace of the schema.</param>
    /// <param name="name">The manifest resource name.</param>
    /// <returns>The last schema registered for the namespace.</returns>
    /// <exception cref="ArgumentNullException">The resource was not found.</exception>
    public static XmlSchema? AddManifestResourceSchema(this XmlSchemaSet xmlSchemaSet,
                                                      Assembly resourceAssembly,
                                                      string targetNamespace,
                                                      string name)
    {
        using var schemaStream = resourceAssembly.GetManifestResourceStream(name);

        return xmlSchemaSet.Add(targetNamespace, schemaStream.Guard(nameof(name)));
    }

    /// <summary>Validates an XML string against schemas and then deserializes it.</summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="serializer">The serializer.</param>
    /// <param name="xml">The XML text, read as ASCII.</param>
    /// <param name="func">Supplies the schema set used for validation.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="System.Xml.Schema.XmlSchemaException">The document does not satisfy the schema.</exception>
    public static T? ValidateAndDeserialize<T>(this XmlSerializer serializer, string xml, Func<XmlSchemaSet> func)
        where T : class
    {
        var data = Encoding.ASCII.GetBytes(xml);
        using var stream = new MemoryStream(data, 0, data.Length);

        return serializer.ValidateAndDeserialize<T>(stream, func);
    }

    /// <summary>Validates XML from a stream against schemas and then deserializes it.</summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="serializer">The serializer.</param>
    /// <param name="stream">The stream holding the XML.</param>
    /// <param name="func">Supplies the schema set used for validation.</param>
    /// <returns>The deserialized object.</returns>
    /// <exception cref="System.Xml.Schema.XmlSchemaException">The document does not satisfy the schema.</exception>
    public static T? ValidateAndDeserialize<T>(this XmlSerializer serializer, Stream stream, Func<XmlSchemaSet> func)
        where T : class
    {
        Validate(stream, func);

        return serializer.Deserialize<T>(stream);
    }

    /// <summary>Serializes an object and validates the result against schemas.</summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="serializer">The serializer.</param>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="func">Supplies the schema set used for validation.</param>
    /// <returns>The serialized text as returned by <see cref="Serialize{T}(XmlSerializer, T)" />.</returns>
    /// <exception cref="System.Xml.Schema.XmlSchemaException">The document does not satisfy the schema.</exception>
    public static string SerializeAndValidate<T>(this XmlSerializer serializer, T obj, Func<XmlSchemaSet> func)
        where T : class
    {
        var xml = serializer.Serialize(obj);
        var data = Encoding.ASCII.GetBytes(xml);
        using var stream = new MemoryStream(data, 0, data.Length);
        Validate(stream, func);

        return xml;
    }

    private static void Validate(Stream stream, Func<XmlSchemaSet> func)
    {
        var xmlReaderSettings = new XmlReaderSettings();
        xmlReaderSettings.Schemas.Add(func());
        xmlReaderSettings.ValidationType = ValidationType.Schema;

        var warningAndErrorsText = string.Empty;
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