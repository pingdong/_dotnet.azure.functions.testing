using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace PingDong.Azure.Functions.Testing
{
    public class FunctionsHost : IDisposable
    {
        private Process _funcHostProcess;

        #region Host

        public int Port { get; private set; }

        protected void Initialize(string dotnetExePath
            , string funcHostExePath
            , string funcAppExePath
            , int port)
        {
            if (string.IsNullOrWhiteSpace(dotnetExePath))
                throw new ArgumentNullException(nameof(dotnetExePath));
            if (string.IsNullOrWhiteSpace(funcHostExePath))
                throw new ArgumentNullException(nameof(funcHostExePath));
            if (string.IsNullOrWhiteSpace(funcAppExePath))
                throw new ArgumentNullException(nameof(funcAppExePath));
            if (port < 0 || port > 65536)
                throw new ArgumentOutOfRangeException(nameof(port));

            if (!File.Exists(dotnetExePath))
                throw new ArgumentOutOfRangeException(nameof(dotnetExePath));
            if (!File.Exists(funcHostExePath))
                throw new ArgumentOutOfRangeException(nameof(funcHostExePath));

            Port = port;

            try
            {
                _funcHostProcess = new Process
                {
                    StartInfo =
                    {
                        FileName = dotnetExePath,
                        Arguments = $"\"{funcHostExePath}\" start -p {port}",
                        WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), funcAppExePath)
                    }
                };

                var success = _funcHostProcess.Start();
                if (!success)
                    throw new InvalidOperationException("Could not start Azure Functions host.");
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Could not start Azure Functions host: {ex.Message}", ex);
            }
        }

        #endregion

        #region Http

        public HttpClient CreateClient(Dictionary<string, string> headers = null)
        {
            if (_funcHostProcess == null)
                throw new InvalidOperationException("Azure Functions host hasn't started");

            var client = new HttpClient();

            if (headers != null && headers.Any())
            {
                foreach (var key in headers.Keys)
                    client.DefaultRequestHeaders.Add(key, headers[key]);
            }

            client.BaseAddress = new Uri($"http://localhost:{Port}");

            return client;
        }

        #endregion

        #region IDispoable

        public virtual void Dispose()
        {
            if (!_funcHostProcess.HasExited)
                _funcHostProcess.Kill();

            _funcHostProcess.Dispose();
        }

        #endregion
    }
}
