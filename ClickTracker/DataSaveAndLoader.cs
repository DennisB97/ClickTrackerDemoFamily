// Copyright 2021 Dennis Baeckstroem 
using System;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace ClickTracker
{
    /// <summary>
    /// DataSaveAndLoader class contains static functions for loading and saving templated data to files in different formats such as binary and xml and txt.
    /// </summary>
    class DataSaveAndLoader
    {
        public static readonly string savePath = Environment.ExpandEnvironmentVariables(@"%LocalAppData%\ClickTracker\");


        public enum EDataReadTypes
        {
            TypeString,
            TypeFloat,
            TypeInt,
            TypeBool
        }


        public static void SaveDataAsBinary<T>(T data, EDataReadTypes type, string fileName)
        {

            using (BinaryWriter writer = new BinaryWriter(File.Open(savePath + fileName, FileMode.OpenOrCreate, FileAccess.Write)))
            {
                switch (type)
                {
                    case EDataReadTypes.TypeString:
                        {
                            if (data is String)
                            {
                                writer.Write(data as string);
                            }

                            break;
                        }

                    case EDataReadTypes.TypeFloat:
                        {
                            if (data is float)
                            {
                                writer.Write((float)(object)data);
                            }

                            break;
                        }

                    case EDataReadTypes.TypeInt:
                        {
                            if (data is int)
                            {
                                writer.Write((int)(object)data);
                            }
                            break;
                        }

                    case EDataReadTypes.TypeBool:
                        {
                            if (data is bool)
                            {
                                writer.Write((bool)(object)data);
                            }
                            break;
                        }

                    default:
                        {
                            break;
                        }

                }


            }


        }

        public static bool DoesFileExist(string fileName)
        {
            return File.Exists(savePath + fileName);
        }


#nullable enable
        public static T? ReadDataFromBinary<T>(EDataReadTypes type, string fileName)
        {

            if (File.Exists(savePath + fileName))
            {

                T? foundData = default(T);
                using (BinaryReader reader = new BinaryReader(File.Open(savePath + fileName, FileMode.Open, FileAccess.Read)))
                {
                    switch (type)
                    {
                        case EDataReadTypes.TypeString:
                            {
                                foundData = (T?)(object)reader.ReadString();
                                break;
                            }

                        case EDataReadTypes.TypeFloat:
                            {
                                foundData = (T?)(object)reader.ReadSingle();
                                break;
                            }

                        case EDataReadTypes.TypeInt:
                            {
                                foundData = (T?)(object)reader.ReadInt32();
                                break;
                            }

                        case EDataReadTypes.TypeBool:
                            {
                                foundData = (T?)(object)reader.ReadBoolean();
                                break;
                            }

                        default:
                            {
                                break;
                            }

                    }

                    return foundData;
                }



            }


            return default(T);
        }

        public static void DeleteFile(string fileName)
        {
            if(DoesFileExist(fileName))
            {
                File.Delete(savePath + fileName);
            }
        }

        public static void SaveDataAsXML<T>(T data, Type type, string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(type);
            TextWriter writer = new StreamWriter(savePath + fileName);
            serializer.Serialize(writer, data);
            writer.Close();
        }

        public static void DataContractSaveAsXML<T>(T data, Type type, string fileName)
        {
            DataContractSerializerSettings settings = new DataContractSerializerSettings();
            DataContractSerializer serializer = new DataContractSerializer(type);
          
            FileStream writer = new FileStream(savePath + fileName, FileMode.Create);
            serializer.WriteObject(writer, data);
            writer.Close();
        }

        public static T? DataContractReadDataFromXML<T>(Type type, string fileName)
        {
            if (DoesFileExist(fileName))
            {
                T? foundData = default(T);
                DataContractSerializer serializer = new DataContractSerializer(type);
                

                using (FileStream fs = new FileStream(savePath + fileName, FileMode.Open))
                {
                    try
                    {
                        foundData = (T?)(Object?)serializer.ReadObject(fs);
                    }
                   catch
                    {
                        //Something modified the file on computer
                    }
                }


                return foundData;
            }


            return default(T);
        }


        public static T? ReadDataFromXML<T>(Type type, string fileName)
        {
            if (DoesFileExist(fileName))
            {
                T? foundData = default(T);
                XmlSerializer serializer = new XmlSerializer(type);

                serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
                serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);

                using (FileStream fs = new FileStream(savePath + fileName, FileMode.Open))
                {
                    try
                    {
                        foundData = (T?)(Object?)serializer.Deserialize(fs);
                    }
                   catch
                    {
                        //Something modified the file on computer
                    }
                }


                return foundData;
            }


            return default(T);
        }


        private static void serializer_UnknownNode(object? sender, XmlNodeEventArgs e)
        {
            Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
        }

        public static string ConvertString(string line)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(line));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                return sb.ToString();
            }
        }
        private static void serializer_UnknownAttribute(object? sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            Console.WriteLine("Unknown attribute " + attr.Name + "='" + attr.Value + "'");
        }


        public static async void SaveText(string text, string fileName)
        {
            using(StreamWriter file = new StreamWriter(savePath + fileName, append: true))
            {
                await file.WriteLineAsync(text);
            }
        }
  
        

    }
}
