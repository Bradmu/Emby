﻿using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller;

namespace MediaBrowser.WebDashboard.Api
{
    public class PackageCreator
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IMemoryStreamFactory _memoryStreamFactory;
        private readonly string _basePath;
        private IResourceFileManager _resourceFileManager;

        public PackageCreator(string basePath, IFileSystem fileSystem, ILogger logger, IServerConfigurationManager config, IMemoryStreamFactory memoryStreamFactory, IResourceFileManager resourceFileManager)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            _config = config;
            _memoryStreamFactory = memoryStreamFactory;
            _basePath = basePath;
            _resourceFileManager = resourceFileManager;
        }

        public async Task<Stream> GetResource(string virtualPath,
            string mode,
            string localizationCulture,
            string appVersion)
        {
            var resourceStream = GetRawResourceStream(virtualPath);

            if (resourceStream != null)
            {
                if (IsFormat(virtualPath, "html"))
                {
                    if (IsCoreHtml(virtualPath))
                    {
                        resourceStream = await ModifyHtml(virtualPath, resourceStream, mode, appVersion, localizationCulture).ConfigureAwait(false);
                    }
                }
            }

            return resourceStream;
        }

        /// <summary>
        /// Determines whether the specified path is HTML.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="format">The format.</param>
        /// <returns><c>true</c> if the specified path is HTML; otherwise, <c>false</c>.</returns>
        private bool IsFormat(string path, string format)
        {
            return Path.GetExtension(path).EndsWith(format, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsCoreHtml(string path)
        {
            if (path.IndexOf(".template.html", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            return IsFormat(path, "html");
        }

        /// <summary>
        /// Modifies the HTML by adding common meta tags, css and js.
        /// </summary>
        /// <returns>Task{Stream}.</returns>
        public async Task<Stream> ModifyHtml(string path, Stream sourceStream, string mode, string appVersion, string localizationCulture)
        {
            using (sourceStream)
            {
                string html;

                using (var memoryStream = new MemoryStream())
                {
                    await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                    var originalBytes = memoryStream.ToArray();

                    html = Encoding.UTF8.GetString(originalBytes, 0, originalBytes.Length);

                    if (!string.IsNullOrWhiteSpace(mode))
                    {
                    }
                    else if (!string.IsNullOrWhiteSpace(path) && !string.Equals(path, "index.html", StringComparison.OrdinalIgnoreCase))
                    {
                        var index = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
                        if (index != -1)
                        {
                            html = html.Substring(index);

                            html = html.Substring(html.IndexOf('>') + 1);

                            index = html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                            if (index != -1)
                            {
                                html = html.Substring(0, index);
                            }
                        }
                        var mainFile = _resourceFileManager.ReadAllText(_basePath, "index.html");

                        html = ReplaceFirst(mainFile, "<div class=\"mainAnimatedPages skinBody\"></div>", "<div class=\"mainAnimatedPages skinBody hide\">" + html + "</div>");
                    }

                    if (!string.IsNullOrWhiteSpace(localizationCulture))
                    {
                        var lang = localizationCulture.Split('-').FirstOrDefault();

                        html = html.Replace("<html", "<html data-culture=\"" + localizationCulture + "\" lang=\"" + lang + "\"");
                    }
                }

                html = html.Replace("<head>", "<head>" + GetMetaTags(mode));

                // Disable embedded scripts from plugins. We'll run them later once resources have loaded
                if (html.IndexOf("<script", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    html = html.Replace("<script", "<!--<script");
                    html = html.Replace("</script>", "</script>-->");
                }

                html = html.Replace("</body>", GetCommonJavascript(mode, appVersion) + "</body>");

                var bytes = Encoding.UTF8.GetBytes(html);

                return new MemoryStream(bytes);
            }
        }

        public string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        /// <summary>
        /// Gets the meta tags.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetMetaTags(string mode)
        {
            var sb = new StringBuilder();

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mode, "android", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src * 'self' 'unsafe-inline' 'unsafe-eval' data: gap: file: filesystem: ws: wss:;\">");
            }
            else
            {
                sb.Append("<meta http-equiv=\"X-UA-Compatibility\" content=\"IE=Edge\">");
            }

            sb.Append("<link rel=\"manifest\" href=\"manifest.json\">");
            sb.Append("<meta name=\"format-detection\" content=\"telephone=no\">");
            sb.Append("<meta name=\"msapplication-tap-highlight\" content=\"no\">");

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=1, user-scalable=no\">");
            }
            else
            {
                sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, minimum-scale=1\">");
            }

            sb.Append("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"application-name\" content=\"Emby\">");
            //sb.Append("<meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\">");

            sb.Append("<meta name=\"robots\" content=\"noindex, nofollow, noarchive\">");

            // Open graph tags
            sb.Append("<meta property=\"og:title\" content=\"Emby\">");
            sb.Append("<meta property=\"og:site_name\" content=\"Emby\">");
            sb.Append("<meta property=\"og:url\" content=\"http://emby.media\">");
            sb.Append("<meta property=\"og:description\" content=\"Energize your media.\">");
            sb.Append("<meta property=\"og:type\" content=\"article\">");
            sb.Append("<meta property=\"fb:app_id\" content=\"1618309211750238\">");

            // http://developer.apple.com/library/ios/#DOCUMENTATION/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html
            sb.Append("<link rel=\"apple-touch-icon\" href=\"touchicon.png\">");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"72x72\" href=\"touchicon72.png\">");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"114x114\" href=\"touchicon114.png\">");
            sb.Append("<link rel=\"apple-touch-startup-image\" href=\"css/images/iossplash.png\">");
            sb.Append("<link rel=\"shortcut icon\" href=\"css/images/favicon.ico\">");
            sb.Append("<meta name=\"msapplication-TileImage\" content=\"touchicon144.png\">");
            sb.Append("<meta name=\"msapplication-TileColor\" content=\"#333333\">");
            sb.Append("<meta name=\"theme-color\" content=\"#43A047\">");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the common javascript.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="version">The version.</param>
        /// <returns>System.String.</returns>
        private string GetCommonJavascript(string mode, string version)
        {
            var builder = new StringBuilder();

            builder.Append("<script>");
            if (!string.IsNullOrWhiteSpace(mode))
            {
                builder.AppendFormat("window.appMode='{0}';", mode);
            }

            else
            {
                builder.AppendFormat("window.dashboardVersion='{0}';", version);
            }

            builder.Append("</script>");

            var versionString = string.IsNullOrWhiteSpace(mode) ? "?v=" + version : string.Empty;

            var files = new List<string>();

            files.Add("scripts/apploader.js" + versionString);

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                files.Insert(0, "cordova.js");
            }

            var tags = files.Select(s => string.Format("<script src=\"{0}\" defer></script>", s)).ToArray(files.Count);

            builder.Append(string.Join(string.Empty, tags));

            return builder.ToString();
        }

        /// <summary>
        /// Gets the raw resource stream.
        /// </summary>
        private Stream GetRawResourceStream(string virtualPath)
        {
            return _resourceFileManager.GetResourceFileStream(_basePath, virtualPath);
        }

    }
}
