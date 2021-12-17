namespace NServiceBus.Persistence.Sql
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    public class PublishDedupeTask : Task
    {
        [Required]
        public ITaskItem[] FilesToPublish { get; set; } = Array.Empty<ITaskItem>();

        [Output]
        public ITaskItem[] FilesToRemove { get; private set; }

        public override bool Execute()
        {
            var scripts = new HashSet<string>();
            var duplicates = new List<ITaskItem>();

            foreach (var item in FilesToPublish)
            {
                var relativePath = item.GetMetadata("RelativePath");

                if (relativePath.StartsWith("NServiceBus.Persistence.Sql") && !relativePath.Contains("Sagas"))
                {
                    if (!scripts.Add(relativePath))
                    {
                        duplicates.Add(item);
                    }
                }
            }

            FilesToRemove = duplicates.ToArray();

            return true;
        }
    }
}
