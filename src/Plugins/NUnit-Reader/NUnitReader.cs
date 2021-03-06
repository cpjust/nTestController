﻿using System;
using NTestController;
using Utilities;
using System.Collections.Generic;
using System.IO;
using NTestController.Factories;
using NUnitReader.Factories;

[assembly: CLSCompliant(true)]
[assembly: System.Runtime.InteropServices.ComVisible(false)]
namespace NUnitReader
{
    public class NUnitReaderPlugin : IReaderPlugin
    {
        public string TestInputFile { get; set; }

        public IList<Test> Tests { get; } = new List<Test>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NUnitReader.NUnitReaderPlugin"/> class.
        /// </summary>
        /// <param name="testInputFile">Test input file.</param>
        public NUnitReaderPlugin(string testInputFile)
        {
            ThrowIf.StringIsNullOrWhiteSpace(testInputFile, nameof(testInputFile));

            TestInputFile = testInputFile;
        }

        #region Inherited from IPlugin

        public string Name => nameof(NUnitReader);
        public PluginType PluginType => PluginType.TestReader;
        public IComputerFactory ComputerFactory { get { return new NUnitComputerFactory(); } }
        public IPlatformFactory PlatformFactory { get { return new NUnitPlatformFactory(); } }

        /// <seealso cref="IPlugin.Execute()"/>
        public bool Execute()
        {
            // Read input file and add to Tests.
            if (!File.Exists(TestInputFile))
            {
                throw new FileNotFoundException("Couldn't find file: {0}".FormatInvariant(TestInputFile));
            }

            var fileLines = File.ReadAllLines(TestInputFile);

            ParseTestInputFile(fileLines);
            return true;
        }

        #endregion Inherited from IPlugin

        /// <summary>
        /// Parses the test input file into a list of NUnitTest objects.
        /// </summary>
        /// <param name="fileLines">An array of lines from the test input file.</param>
        private void ParseTestInputFile(string[] fileLines)
        {
            foreach (string fileLine in fileLines)
            {
                string line = fileLine.Trim();

                // Skip blank lines or commented out lines.
                if (string.IsNullOrWhiteSpace(line) || line.StartsWithInvariant("#"))
                {
                    continue;
                }

                var lineParts = line.Split(new char[] {'|'}, count: 2);

                if (lineParts.Length != 2)
                {
                    throw new InvalidDataException(
                        "Expected 2 parts ('|' separated) but found {0} parts!".FormatInvariant(lineParts.Length));
                }

                var testInput = new NUnitTest(lineParts[0].Trim(), lineParts[1].Trim());

                Tests.Add(testInput);
            }
        }
    }
}

