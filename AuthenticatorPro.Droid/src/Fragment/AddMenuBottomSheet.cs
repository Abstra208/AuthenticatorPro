// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class AddMenuBottomSheet : BottomSheet
    {
        public event EventHandler QrCodeClicked;
        public event EventHandler EnterKeyClicked;
        public event EventHandler RestoreClicked;
        public event EventHandler ImportClicked;

        public AddMenuBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
                new List<SheetMenuItem>
                {
                    new(Resource.Drawable.ic_qr_code, Resource.String.scanQrCode, QrCodeClicked),
                    new(Resource.Drawable.ic_key, Resource.String.enterKey, EnterKeyClicked),
                    new(Resource.Drawable.ic_restore, Resource.String.restoreBackup, RestoreClicked),
                    new(Resource.Drawable.ic_input, Resource.String.importFromOtherApps, ImportClicked)
                });

            return view;
        }
    }
}