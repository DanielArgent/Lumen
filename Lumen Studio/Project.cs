﻿using System;
using System.Collections.Generic;

namespace Lumen.Studio {
    public class Project {
        public String Name { get; set; }
        public String Path { get; set; }

        public Dictionary<String, Object> Metadata { get; set; }

        public ProjectType Type { get; set; }
    }

    public abstract class ProjectType {
        public String Name { get; set; }
        public Language Language { get; set; }

        public abstract Project InitNewProject(String projectName, Dictionary<String, String> options);

        public abstract IRunResult Build(Project project);
    }
}
