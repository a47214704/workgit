using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GroupPay.Models;
using Core.Data;

namespace GroupPay
{
    public class SystemInitializer
    {
        public SystemInitializer(SiteConfig config, DataAccessor dataAccessor)
        {
            this.Config = config ?? throw new ArgumentNullException(nameof(config));
            this.DataAccessor = dataAccessor ?? throw new ArgumentNullException(nameof(dataAccessor));
        }

        private SiteConfig Config { get; set; }

        private DataAccessor DataAccessor { get; set; }

        public async Task Initialize()
        {
            List<ConfigItem> configs = await this.DataAccessor.GetAll<ConfigItem>("select * from `site_config`");
            foreach (ConfigItem config in configs)
            {
                this.Config.Apply(config);
            }
        }
    }
}
