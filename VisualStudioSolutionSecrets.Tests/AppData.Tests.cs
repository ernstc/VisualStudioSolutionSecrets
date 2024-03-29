﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.IO;
using Xunit;


namespace VisualStudioSolutionSecrets.Tests
{
    [Collection("vs-secrets Tests")]
    public class AppDataTests : IDisposable
    {

        const string FILE_NAME = "testAppData.json";
        const string REFERENCE_FILE_NAME = "configFile.json";


        private class SampleData
        {
            public int IntValue { get; set; }
            public string StringValue { get; set; } = null!;
            public DateTime DateTimeValue { get; set; }
            public DateTimeOffset DateTimeOffsetValue { get; set; }
            public bool BoolValue { get; set; }
            public int[] IntArrayValue { get; set; } = null!;
            public InnerSampleData ObjectValue { get; set; } = null!;
        }


        private class InnerSampleData
        {
            public int IntValue { get; set; }
            public string? StringValue { get; set; }
        }


        private static SampleData GetSampleData()
        {
            return new SampleData
            {
                IntValue = 1,
                StringValue = "string",
                DateTimeValue = new DateTime(2022, 09, 16, 16, 00, 01),
                DateTimeOffsetValue = new DateTimeOffset(2022, 09, 16, 16, 00, 01, TimeSpan.FromHours(2)),
                BoolValue = true,
                IntArrayValue = new int[] { 100, 200 },
                ObjectValue = new InnerSampleData
                {
                    IntValue = 2,
                    StringValue = null
                }
            };
        }


        public AppDataTests()
        {
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.ConfigFilesPath);

            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);
        }


        public void Dispose()
        {
            string filePath = Path.Combine(Constants.ConfigFilesPath, FILE_NAME);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }


        [Fact]
        public void SaveData_Test()
        {
            SampleData data = GetSampleData();

            AppData.SaveData(FILE_NAME, data);

            string filePath = Path.Combine(Constants.ConfigFilesPath, FILE_NAME);

            Assert.True(File.Exists(filePath));

            string referenceFilePath = Path.Combine(Constants.ConfigFilesPath, REFERENCE_FILE_NAME);
            var referenceContent = File.ReadAllLines(referenceFilePath);
            var content = File.ReadAllLines(filePath);

            Assert.Equal(referenceContent, content);
        }


        [Fact]
        public void LoadData_Test()
        {
            SampleData data = GetSampleData();

            var loadedData = AppData.LoadData<SampleData>(REFERENCE_FILE_NAME);

            Assert.Equivalent(data, loadedData, strict: true);
        }
    }
}
