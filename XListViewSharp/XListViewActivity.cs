using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using Java.Lang;
using XListViewSharp;
using String = System.String;

namespace XListViewSharp
{
    [Activity(Label = "XListViewSharp", MainLauncher = true, Icon = "@drawable/icon")]
    public class XListViewActivity : Activity, XListView.IXListViewListener
    {
            private XListView mListView;
            private ArrayAdapter<String> mAdapter;
            private List<String> items = new List<String>();
            private Handler mHandler;
            private int start = 0;
            private static int refreshCnt = 0;

            protected override void OnCreate(Bundle savedInstanceState) {
                base.OnCreate(savedInstanceState);
                SetContentView(Resource.Layout.main);
                GenItems();
                mListView = (XListView) FindViewById(Resource.Id.xListView);
                //findViewById(R.id.xListView);
                mListView.SetPullLoadEnable(true);
            mAdapter = new ArrayAdapter<String>(this,  Resource.Layout.list_item, items);
                mListView.Adapter = mAdapter;
                //      mListView.setPullLoadEnable(false);
                //      mListView.setPullRefreshEnable(false);
                mListView.SetXListViewListener(this);
                mHandler = new Handler();
            }

            private void GenItems() {
                for (int i = 0; i != 20; ++i) {
                    items.Add("refresh cnt " + (++start));
                }
            }

            private void OnLoad() {
                mListView.StopRefresh();
                mListView.StopLoadMore();
                mListView.SetRefreshTime("刚刚");
            }

            public void OnRefresh()
            {
                mHandler.PostDelayed(new Runnable(() =>
                {

                    start = ++refreshCnt;
                    items.Clear();
                    GenItems();
                    // mAdapter.notifyDataSetChanged();
                    mAdapter = new ArrayAdapter<String>(this, Resource.Layout.list_item, items);

                    mListView.Adapter = (mAdapter);
                    OnLoad();
                    
                }), 2000);
            }

            public void OnLoadMore() {
                mHandler.PostDelayed(new Runnable(() =>
                {
                    GenItems();
                    mAdapter.NotifyDataSetChanged();
                    OnLoad();
                }), 2000);
            }
    }
}


