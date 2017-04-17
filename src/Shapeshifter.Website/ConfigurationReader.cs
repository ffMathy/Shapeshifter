using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Website
{
    using System.IO;

    using Microsoft.Extensions.Configuration;

    public class ConfigurationReader
    {
        readonly IConfigurationRoot _configuration;

        public ConfigurationReader(string path)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path)
                .AddEnvironmentVariables("shapeshifter:");

            _configuration = builder.Build();
        }

        public string Read(string key)
        {
            return _configuration[key];
        }
    }
}
