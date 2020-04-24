﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameframe.SaveLoad
{
    /// <summary>
    /// SaveLoadManager
    /// Manager for saving and loading objects to/from disk.
    /// </summary>
    [CreateAssetMenu(menuName = "GameFrame/SaveLoad/SaveLoadManager")]
    public class SaveLoadManager : ScriptableObject
    {
        [Header("Directories"),SerializeField] private string defaultFolder = "SaveData";
        public string DefaultFolder => defaultFolder;
        
        [SerializeField] private string baseFolder = "GameData";
        public string BaseFolder => baseFolder;
        
        [Header("Save Method"),SerializeField] private SerializationMethodType saveMethod = SerializationMethodType.Default;

        [Header("Encryption"),SerializeField] protected string key = string.Empty;
        public string Key => key;
        
        [SerializeField] protected string salt = string.Empty;
        public string Salt => salt;
        
        private Dictionary<SerializationMethodType, ISerializationMethod> _methods = null;

        private void OnEnable()
        {
            //OnEnabled will be called when entering play mode in editor but also when selecting the object in editor
            //Constructor may only be called once which may lead to some weird behaviour if we use it for initializing this dictionary
            //Using OnEnabled ensures we do this initialization and the dictionary is fresh when we hit the play button in editor
            _methods = new Dictionary<SerializationMethodType, ISerializationMethod>();
        }

        /// <summary>
        /// Helper Method for constructing a new instance of SaveLoadManager which specific protected settings
        /// </summary>
        /// <param name="baseFolder">Base directory folder</param>
        /// <param name="defaultFolder">Default folder to save files to</param>
        /// <param name="saveMethod">Method to use to save and load files</param>
        /// <param name="key">Encryption key is required if using an encrypted method.</param>
        /// <param name="salt">Encryption salt is required if using an ecrypted method.</param>
        /// <returns>Newly created instance of SaveLoadManager</returns>
        public static SaveLoadManager Create(string baseFolder, string defaultFolder, SerializationMethodType saveMethod, string key = null, string salt = null)
        {
            var instance = CreateInstance<SaveLoadManager>();

            instance.baseFolder = baseFolder;
            instance.defaultFolder = defaultFolder;
            instance.key = key;
            instance.salt = salt;
            instance.saveMethod = saveMethod;
            
            return instance;
        }
        
        /// <summary>
        /// Save an object to disk
        /// </summary>
        /// <param name="obj">object to be saved</param>
        /// <param name="filename">Name of file that will be written to</param>
        /// <param name="folder">Name of the folder that should contain the file</param>
        public void Save(object obj, string filename, string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
            {
                folder = defaultFolder;
            }
            var saveLoadMethod = GetSaveLoadMethod(saveMethod);
            SaveLoadUtility.Save(obj,saveLoadMethod,filename,folder, baseFolder);
        }
        
        /// <summary>
        /// Load an object from disk
        /// </summary>
        /// <param name="filename">Name of file to load from</param>
        /// <param name="folder">Name of folder containing the file</param>
        /// <typeparam name="T">Type of object to be loaded from file</typeparam>
        /// <returns>Instance of object loaded from file</returns>
        public T Load<T>(string filename, string folder = null)
        {
            return (T)Load(typeof(T), filename, folder);
        }

        /// <summary>
        /// Load an object from disk
        /// </summary>
        /// <param name="type">Type of object to be loaded</param>
        /// <param name="filename">Name of file to load object from</param>
        /// <param name="folder">Name of folder containing the file to be loaded</param>
        /// <returns>Instance of object to be loaded. Null if file did not exist.</returns>
        public object Load(Type type, string filename, string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
            {
                folder = defaultFolder;
            }
            var saveLoadMethod = GetSaveLoadMethod(saveMethod);
            return SaveLoadUtility.Load(type, saveLoadMethod,filename,folder, baseFolder);
        }

        /// <summary>
        /// Delete saved file from disk
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="folder"></param>
        public void DeleteSave(string filename, string folder = null)
        {
            if (string.IsNullOrEmpty(folder))
            {
                folder = defaultFolder;
            }
            SaveLoadUtility.DeleteSavedFile(filename,folder, baseFolder);
        }
        
        /// <summary>
        /// Save object to file and specify the method of save/load
        /// </summary>
        /// <param name="methodType">Method to be used to save the file to disk.</param>
        /// <param name="obj">Object to be written to disk.</param>
        /// <param name="filename">Name of file to write to.</param>
        /// <param name="folder">Name of folder to save to. If null the default folder will be used.</param>
        public void SaveWithMethod(SerializationMethodType methodType, object obj, string filename, string folder = null)
        {
            var saveLoadMethod = GetSaveLoadMethod(methodType);
            SaveLoadUtility.Save(obj,saveLoadMethod,filename,folder);
        }

        /// <summary>
        /// Load object from file and specify the method of save/load
        /// </summary>
        /// <param name="methodType">Method to be used to save the file to disk.</param>
        /// <param name="filename">Name of file to be read from.</param>
        /// <param name="folder">Name of the folder containing the file.</param>
        /// <typeparam name="T">Type of object to be loaded from the file.</typeparam>
        /// <returns>Object instance loaded from file. Null if file does not exist or load failed.</returns>
        public object LoadWithMethod<T>(SerializationMethodType methodType, string filename, string folder = null)
        {
            return (T)LoadWithMethod(methodType, typeof(T), filename, folder);
        }

        /// <summary>
        /// Load object from file and specify the method of save/load
        /// </summary>
        /// <param name="methodType">Method to be used to save the file to disk.</param>
        /// <param name="filename">Name of file to be read from.</param>
        /// <param name="folder">Name of the folder containing the file.</param>
        /// <typeparam name="T">Type of object to be loaded from the file.</typeparam>
        /// <returns>Object instance loaded from file. Null if file does not exist or load failed.</returns>
        public object LoadWithMethod(SerializationMethodType methodType, Type type, string filename, string folder = null)
        {
            var saveLoadMethod = GetSaveLoadMethod(methodType);
            return SaveLoadUtility.Load(type, saveLoadMethod,filename,folder);
        }

        public bool IsEncrypted => (saveMethod == SerializationMethodType.BinaryEncrypted || saveMethod == SerializationMethodType.JsonEncrypted);
        
        private ISerializationMethod GetSaveLoadMethod(SerializationMethodType methodType)
        {
            if (_methods == null)
            {
                _methods = new Dictionary<SerializationMethodType, ISerializationMethod>();
            }

            if (_methods.TryGetValue(methodType, out var method))
            {
                return method;
            }

            //Create method if it did not yet exist
            switch (methodType)
            {
                case SerializationMethodType.Default:
                    method = GetSaveLoadMethod(SerializationMethodType.Json);
                    break;
                case SerializationMethodType.Binary:
                    method = new SerializationMethodBinary();
                    break;
                case SerializationMethodType.Json:
                    method = new SerializationMethodJson();
                    break;
                case SerializationMethodType.BinaryEncrypted:
                    method = new SerializationMethodBinaryEncrypted(key,salt);
                    break;
                case SerializationMethodType.JsonEncrypted:
                    method = new SerializationMethodJsonEncrypted(key,salt);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(methodType), methodType, "SaveLoadMethodType not supported");
            }

            _methods[methodType] = method;
            
            return method;
        }
        
    }    
}


