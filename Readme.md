# GSD

## What does GSD Stand For?

It is just a combination of three letters. It _could_ mean "Git Service Daemon"
because we are monitoring your Git repo and keeping it up to date with the
remote. It _could_ mean "Get Stuff Done" because it keeps Git out of the way
and just does what you need it to do.

## What does GSD Do?

* `gsd clone <url> <dir>` will create a local clone of the given `url` inside
  `<dir>/src`. Some config data will be in `<dir>/.gvfs`.

* Your initial clone will check out the `master` branch and cause a bunch of
  blob downloads as you fill out the data. We will only download blobs as you
  need them in a checkout. This differs from [VFS for Git](https://github.com/microsoft/vfsforgit)
  in that there is no filesystem driver filling in files on-demand. This acts
  more like Git LFS.

## What will GSD Do?

Here are some things we plan to do as we work on this project. These will likely
be breaking changes as we go:

1. Allow a clone to initialize a sparse-checkout file that is very small initially.

1. Expand and contract the sparse-checkout on (user) demand.

1. Automatically set up fsmonitor.

1. Performance improvements around sparse-checkout.

1. Remove the .gvfs metadata folder and put everything in Git config.

1. Remove all logic from the GSD.Mount process and into GSD.Service.

1. Allow batched requests from the read-object hook.

1. Allow GSD.Service to monitor and maintain vanilla Git repos.

1. Integrate fsmonitor with status caching.

## Licenses

The VFS for Git source code in this repo is available under the MIT license. See [License.md](License.md).

VFS for Git relies on the PrjFlt filter driver, formerly known as the GvFlt filter driver, available as a prerelease NuGet package.
