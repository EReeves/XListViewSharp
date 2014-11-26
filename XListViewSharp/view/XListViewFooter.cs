
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Views.Animations;

namespace XListViewSharp
{
    public class XListViewFooter : LinearLayout
    {
            public const int STATE_NORMAL = 0;
            public const int STATE_READY = 1;
            public const int STATE_LOADING = 2;

            private Context mContext;

            private View mContentView;
            private View mProgressBar;
            private TextView mHintView;

            public XListViewFooter(Context context) : base(context) {       
                InitView(context);
            }

            public XListViewFooter(Context context, IAttributeSet attrs) : base(context) {
                InitView(context);
            }


            public void SetState(int state) {
            mHintView.Visibility = ViewStates.Invisible;//.setVisibility(View.INVISIBLE);
            mProgressBar.Visibility = ViewStates.Invisible;//setVisibility(View.INVISIBLE);
                 mHintView.Visibility = ViewStates.Invisible;//setVisibility(View.INVISIBLE);
                if (state == STATE_READY) {
                mHintView.Visibility = ViewStates.Visible;//setVisibility(View.VISIBLE);
                mHintView.Text = Resources.GetString(Resource.String.xlistview_header_hint_ready);//(R.string.xlistview_footer_hint_ready);
                } else if (state == STATE_LOADING) {
                mProgressBar.Visibility = ViewStates.Visible;//setVisibility(View.VISIBLE);
                } else {
                mHintView.Visibility = ViewStates.Visible;//setVisibility(View.VISIBLE);
                mHintView.Text = Resources.GetString(Resource.String.xlistview_header_hint_normal);//(R.string.xlistview_footer_hint_normal);
                }
            }

            public void SetBottomMargin(int height) {
                if (height < 0) return ;
                    LinearLayout.LayoutParams lp = (LinearLayout.LayoutParams)mContentView.LayoutParameters;//getLayoutParams();
                    lp.BottomMargin = height;
                    mContentView.LayoutParameters = lp;
            }

            public int GetBottomMargin() {
                LinearLayout.LayoutParams lp = (LinearLayout.LayoutParams)mContentView.LayoutParameters;
                return lp.BottomMargin;
            }


            /**
     * normal status
     */
            public void Normal() {
                mHintView.Visibility = ViewStates.Visible;//View.VISIBLE);
                mProgressBar.Visibility = ViewStates.Gone;//setVisibility(View.GONE);
            }


            /**
     * loading status 
     */
            public void Loading() {
            mHintView.Visibility = ViewStates.Gone; //setVisibility(View.GONE);
            mProgressBar.Visibility = ViewStates.Visible;//.setVisibility(View.VISIBLE);
            }

            /**
     * hide footer when disable pull load more
     */
            public void Hide() {
                LinearLayout.LayoutParams lp = (LinearLayout.LayoutParams)mContentView.LayoutParameters;
                lp.Height = 0;
                mContentView.LayoutParameters =lp;
            }

            /**
     * show footer
     */
            public void Show() {
                LinearLayout.LayoutParams lp = (LinearLayout.LayoutParams)mContentView.LayoutParameters;
                lp.Height = ViewGroup.LayoutParams.WrapContent;// LayoutParams.WRAP_CONTENT;
                mContentView.LayoutParameters = lp;
            }

            private void InitView(Context context) {
                mContext = context;
                LinearLayout moreView = (LinearLayout)LayoutInflater.From(mContext).Inflate(Resource.Layout.xlistview_footer, null);//R.layout.xlistview_footer, null);
                this.AddView(moreView);//      addView(moreView);
                moreView.LayoutParameters = (new LinearLayout.LayoutParams(ViewGroup.LayoutParams.FillParent, ViewGroup.LayoutParams.WrapContent));

                mContentView = moreView.FindViewById(Resource.Id.xlistview_header_content);//R.id.xlistview_footer_content);
                mProgressBar = moreView.FindViewById(Resource.Id.xlistview_footer_progressbar);//.findViewById(R.id.xlistview_footer_progressbar);
                mHintView = (TextView)moreView.FindViewById(Resource.Id.xlistview_footer_hint_textview);//findViewById(R.id.xlistview_footer_hint_textview);
            }
     }
}


