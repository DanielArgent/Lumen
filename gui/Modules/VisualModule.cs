﻿using Lumen.Lang;

namespace Lumen.Lang.Libraries.Visual {
    public class Visual : Module {
        public static TypeClass Control { get; } = new ControlTC();

        public static FormModule RForm { get; } = new FormModule();

        public static ColorModule ColorType { get; } = new ColorModule();

        public static ButtonModule Button { get; } = new ButtonModule();
        public static Module Label { get; } = new LabelModule();
        public static Module Style { get; } = new StyleModule();
        public static Module RTB { get; } = new RTBModule();
        public static Module Dock { get; } = new DockModule();

        public static Visual Instance { get; } = new Visual();

        private Visual() {
            this.SetField("Control", Control);
            this.SetField("Form", RForm);
            this.SetField("Color", ColorType);
            this.SetField("Button", Button);
            this.SetField("Label", Label);
            this.SetField("Style", Style);
            this.SetField("RichTextBox", RTB);
            this.SetField("Dock", Dock);
        }
    }
}
