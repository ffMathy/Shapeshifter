﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shapeshifter.Website
{
    using System.IO;

    using Microsoft.Extensions.Configuration;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	public class ConfigurationReader : IConfigurationReader
  {
        readonly JObject _configuration;

        public ConfigurationReader(string path)
        {
			_configuration = JsonConvert.DeserializeObject<JObject>(
				File.ReadAllText(path));
        }

        public string Read(string key)
        {
			var target = _configuration;

			var split = key.Split(".");
			for(var i=0;i<split.Length;i++) {
				var property = split[i];
				if(i == split.Length - 1)
					return target.GetValue(property).Value<string>();

				target = target.GetValue(property).Value<JObject>();
			}

			return null;
        }
    }
}
