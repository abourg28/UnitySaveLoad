#if JSON_DOT_NET

using System;
using System.IO;
using Newtonsoft.Json;

namespace Gameframe.SaveLoad
{
    public class SerializationMethodJsonDotNet : ISerializationMethod
    {
        protected JsonConverter[] converters;

        public void SetConverters(params JsonConverter[] converters)
        {
            this.converters = converters;
        }

        public void Save(object savedObject, FileStream fileStream)
        {
            var json = JsonConvert.SerializeObject(savedObject, converters);
            using (var streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.Write(json);
                streamWriter.Close();
            }
        }

        public object Load(Type savedObjectType, FileStream fileStream)
        {
            object loadedObj = null;
            
            using (var streamReader = new StreamReader(fileStream))
            {
                var json = streamReader.ReadToEnd();
                streamReader.Close();
                loadedObj = JsonConvert.DeserializeObject(json,savedObjectType, converters);
            }
            
            return loadedObj;
        }

        public object Copy(object copyObject)
        {
            using (var stream = new MemoryStream())
            {
                var writeJson = JsonConvert.SerializeObject(copyObject, converters);
            
                var streamWriter = new StreamWriter(stream);
                streamWriter.Write(writeJson);
                streamWriter.Flush();

                stream.Position = 0;

                using (var streamReader = new StreamReader(stream))
                {
                    var readJson = streamReader.ReadToEnd();
                    return JsonConvert.DeserializeObject(readJson, copyObject.GetType(), converters);
                }
            }
        }
    }
}

#endif