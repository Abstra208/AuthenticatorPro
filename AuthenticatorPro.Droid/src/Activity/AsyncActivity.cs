// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Activity
{
    internal abstract class AsyncActivity : BaseActivity, IDisposable
    {
        private readonly SemaphoreSlim _onCreateLock;
        private readonly SemaphoreSlim _onResumeLock;
        private bool _isDisposed;

        protected AsyncActivity(int layout) : base(layout)
        {
            _onCreateLock = new SemaphoreSlim(1, 1);
            _onResumeLock = new SemaphoreSlim(1, 1);
        }

        ~AsyncActivity()
        {
            Dispose(false);
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _onCreateLock.Dispose();
                    _onResumeLock.Dispose();
                }

                _isDisposed = true;
            }

            base.Dispose(disposing);
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            await _onCreateLock.WaitAsync();
            await OnCreateAsync(savedInstanceState);
            _onCreateLock.Release();
        }

        protected abstract Task OnCreateAsync(Bundle savedInstanceState);

        protected override async void OnResume()
        {
            base.OnResume();

            await _onCreateLock.WaitAsync();
            _onCreateLock.Release();

            await _onResumeLock.WaitAsync();
            await OnResumeAsync();
            _onResumeLock.Release();
        }

        protected abstract Task OnResumeAsync();

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode,
            Intent intent)
        {
            base.OnActivityResult(requestCode, resultCode, intent);

            await _onResumeLock.WaitAsync();
            _onResumeLock.Release();

            await OnActivityResultAsync(requestCode, resultCode, intent);
        }

        protected abstract Task
            OnActivityResultAsync(int requestCode, [GeneratedEnum] Result resultCode, Intent intent);
    }
}