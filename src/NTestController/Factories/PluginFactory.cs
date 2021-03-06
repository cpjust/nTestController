﻿using System;
using System.Xml;
using Utilities;
using System.Reflection;
using System.IO;

namespace NTestController
{
    public static class PluginFactory
    {
        /// <summary>
        /// Gets the plugin specified in the XmlNode.
        /// </summary>
        /// <param name="node">The XmlNode containing the path to the plugin DLL.</param>
        /// <param name="xmlConfig">The NTestController.xml config file.</param>
        /// <returns>The plugin.</returns>
        /// <exception cref="ArgumentNullException">A null or whitespace only argument was passed in.</exception>
        /// <exception cref="ArgumentException">The XML node contains an unknown plugin type.</exception>
        /// <exception cref="TypeLoadException">The plugin DLL doesn't contain the plugin type specified in the XML node.</exception>
        /// <exception cref="XmlException">There was an error parsing the XmlNode.</exception>
        public static IPlugin GetPlugin(XmlNode node, string xmlConfig)
        {
            ThrowIf.ArgumentNull(node, nameof(node));

            string dllFile = XmlUtils.GetXmlAttribute(node, "path");
            string pluginTypeString = XmlUtils.GetXmlAttribute(node, "type");
            PluginType pluginType;
            const bool ignoreCase = true;

            if (!Enum.TryParse(pluginTypeString, ignoreCase, out pluginType))
            {
                throw new ArgumentException(StringUtils.FormatInvariant("Invalid Plugin Type '{0}' was found.", pluginTypeString));
            }

            // Load the plugin.
            IPlugin plugin = LoadPlugin(dllFile, xmlConfig);

            if (plugin.PluginType != pluginType)
            {
                throw new TypeLoadException(StringUtils.FormatInvariant("Wrong plugin type found!  Expecting '{0}', but got '{1}' in '{2}'.",
                    pluginTypeString, plugin.PluginType.ToString(), dllFile));
            }

            return plugin;
        }

        /// <summary>
        /// Loads the plugin.
        /// </summary>
        /// <param name="dllFile">The path to the DLL file.</param>
        /// <param name="xmlConfig">The NTestController.xml config path and filename.</param>
        /// <returns>The plugin.</returns>
        /// <exception cref="ArgumentNullException">A null or whitespace only argument was passed in.</exception>
        /// <exception cref="DllNotFoundException">The DLL doesn't exist.</exception>
        /// <exception cref="EntryPointNotFoundException">The DLL doesn't implement the IPlugin interface.</exception>
        public static IPlugin LoadPlugin(string dllFile, string xmlConfig)
        {
            ThrowIf.StringIsNullOrWhiteSpace(dllFile, nameof(dllFile));
            ThrowIf.StringIsNullOrWhiteSpace(xmlConfig, nameof(xmlConfig));
            
            if (!File.Exists(dllFile))
            {
                throw new DllNotFoundException(StringUtils.FormatInvariant("Could not find: '{0}'!", dllFile));
            }

            IPlugin plugin = null;

            AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
            Assembly assembly = Assembly.Load(an);
            Type[] types = assembly.GetTypes();

            foreach (Type type in types) 
            { 
                if (type.IsInterface || type.IsAbstract) 
                { 
                    continue; 
                } 
                else 
                { 
                    if (type.GetInterface(typeof(IPluginFactory).FullName) != null) 
                    { 
                        IPluginFactory pluginFactory = (IPluginFactory)Activator.CreateInstance(type);
                        plugin = pluginFactory.GetPlugin(xmlConfig);
                    } 
                } 
            }

            if (plugin == null)
            {
                throw new EntryPointNotFoundException(StringUtils.FormatInvariant("No implementations of IPlugin were found in: '{0}'!", dllFile));
            }

            return plugin;
        }
    }
}

