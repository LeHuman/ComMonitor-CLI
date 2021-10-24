using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace ComPlotter.Util {

    public class AssemblyInformation {

        /// <summary>
        ///     Gets or sets the product to which this assembly belongs.
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        ///     Gets or sets the title of the product.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets the copyright string for this product.
        /// </summary>
        public string Copyright { get; set; }

        /// <summary>
        ///     Gets or sets a user-friendly representation of the version of the product.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///     Gets or sets a descriptive overview of the product.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///     Gets or sets the owner or rights holder of the product.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        ///     Gets or sets an image to display.
        /// </summary>
        public BitmapImage Image { get; set; }

        /// <summary>
        ///     Gets or sets a string and Uri tuple representing a repository link.
        /// </summary>
        public Tuple<string, Uri> RepoLink { get; set; }

        /// <summary>
        ///     Constructs a new AssemblyInformation object with information contained in the given assembly
        /// </summary>
        /// <param name="assembly">The assembly to extract information from</param>
        public AssemblyInformation(Assembly assembly) {
            Title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            Product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            Owner = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version.ToString();
        }
    }
}
