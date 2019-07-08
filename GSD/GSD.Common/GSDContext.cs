using GSD.Common.FileSystem;
using GSD.Common.Git;
using GSD.Common.Tracing;
using System;

namespace GSD.Common
{
    public class GSDContext : IDisposable
    {
        private bool disposedValue = false;

        public GSDContext(ITracer tracer, PhysicalFileSystem fileSystem, GitRepo repository, GSDEnlistment enlistment)
        {
            this.Tracer = tracer;
            this.FileSystem = fileSystem;
            this.Enlistment = enlistment;
            this.Repository = repository;

            this.Unattended = GSDEnlistment.IsUnattended(this.Tracer);
        }

        public ITracer Tracer { get; private set; }
        public PhysicalFileSystem FileSystem { get; private set; }
        public GitRepo Repository { get; private set; }
        public GSDEnlistment Enlistment { get; private set; }
        public bool Unattended { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.Repository.Dispose();
                    this.Tracer.Dispose();
                    this.Tracer = null;
                }

                this.disposedValue = true;
            }
        }
    }
}
