
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Android.Animation;
using Java.Interop;
using XListViewSharp;

namespace XListViewSharp
{
    //Most of this is pulled straight from the java src, since the syntax is so similar.
    public class XListView : ListView, ListView.IOnScrollListener
    {

        private float mLastY = -1; // save event y
        private Scroller mScroller; // used for scroll back
        private IOnScrollListener mScrollListener; // user's scroll listener

        // the interface to trigger refresh and load more.
        private IXListViewListener mListViewListener;

        // -- header view
        private XListViewHeader mHeaderView;
        // header view content, use it to calculate the Header's height. And hide it
        // when disable pull refresh.
        private RelativeLayout mHeaderViewContent;
        private TextView mHeaderTimeView;
        private int mHeaderViewHeight; // header view's height
        private bool mEnablePullRefresh = true;
        private bool mPullRefreshing = false; // is refreashing.

        // -- footer view
        private XListViewFooter mFooterView;
        private bool mEnablePullLoad;
        private bool mPullLoading;
        private bool mIsFooterReady = false;

        // total list items, used to detect is at the bottom of listview.
        private int mTotalItemCount;

        // for mScroller, scroll back from header or footer.
        private int mScrollBack;
        private const int SCROLLBACK_HEADER = 0;
        private const int SCROLLBACK_FOOTER = 1;

        private const int SCROLL_DURATION = 400; // scroll back duration
        private const int PULL_LOAD_MORE_DELTA = 50; // when pull up >= 50px
        // at bottom, trigger
        // load more.
        private const float OFFSET_RADIO = 1.8f; // support iOS like pull
        // feature.

        public XListView(Context context) : base(context) {
            InitWithContext(context);
        }

        public XListView(Context context, IAttributeSet attrs) : base(context, attrs) {
            InitWithContext(context);
        }

        public XListView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle) {
            InitWithContext(context);

        }

        private void InitWithContext(Context context) {
            mScroller = new Scroller(context, new Android.Views.Animations.DecelerateInterpolator());
            // XListView need the scroll event, and it will dispatch the event to
            // user's listener (as a proxy).
            base.SetOnScrollListener(this);

            // init header view
            mHeaderView = new XListViewHeader(context);
            mHeaderViewContent = (RelativeLayout)mHeaderView.FindViewById<RelativeLayout>(Resource.Id.xlistview_footer_content);
            mHeaderTimeView = (TextView) mHeaderView.FindViewById<TextView>(Resource.Id.xlistview_header_time);

            AddHeaderView(mHeaderView);

            // init footer view
            mFooterView = new XListViewFooter(context);

            GlobalLayoutListener listener = null;
            listener = new GlobalLayoutListener(new Action(() =>
            {
                mHeaderViewHeight = mHeaderViewContent.Height;
                mHeaderView.ViewTreeObserver.RemoveGlobalOnLayoutListener(listener);
            }));
            // init header heigh
            mHeaderView.ViewTreeObserver.AddOnGlobalLayoutListener(listener);

            /*
                .getViewTreeObserver().addOnGlobalLayoutListener(

                @Override
                public void onGlobalLayout() {
                    mHeaderViewHeight = mHeaderViewContent.getHeight();
                    getViewTreeObserver()
                        .removeGlobalOnLayoutListener(this);
                }
            });*/
        }

        public override IListAdapter Adapter
        {
            get
            {
                return base.Adapter;
            }
            set
            {
                if (mIsFooterReady == false) {
                    mIsFooterReady = true;
                    AddFooterView(mFooterView);
                }
                base.Adapter = value;
            }
        }

        /**
     * enable or disable pull down refresh feature.
     * 
     * @param enable
     */
        public void SetPullRefreshEnable(bool enable) {
            mEnablePullRefresh = enable;
            if (!mEnablePullRefresh) { // disable, hide the content
                mHeaderViewContent.Visibility = ViewStates.Invisible;
            } else {
                mHeaderViewContent.Visibility = ViewStates.Visible;
            }
        }

        /**
     * enable or disable pull up load more feature.
     * 
     * @param enable
     */
        public void SetPullLoadEnable(bool enable) {
            mEnablePullLoad = enable;
            if (!mEnablePullLoad) {
                mFooterView.Hide();
                mFooterView.SetOnClickListener(null);
            } else {
                mPullLoading = false;
                mFooterView.Show();
                mFooterView.SetState(XListViewFooter.STATE_NORMAL);
                // both "pull up" and "click" will invoke load more.

                mFooterView.SetOnClickListener(new OnClickListener(StartLoadMore));

                /*mFooterView.setOnClickListener(new OnClickListener() {
                    @Override
                    public void onClick(View v) {
                        startLoadMore();
                    }
                });*/
            }
        }

        /**
     * stop refresh, reset header view.
     */
        public void StopRefresh() {
            if (mPullRefreshing == true) {
                mPullRefreshing = false;
                ResetHeaderHeight();
            }
        }

        /**
     * stop load more, reset footer view.
     */
        public void StopLoadMore() {
            if (mPullLoading == true) {
                mPullLoading = false;
                mFooterView.SetState(XListViewFooter.STATE_NORMAL);
            }
        }

        /**
     * set last refresh time
     * 
     * @param time
     */
        public void SetRefreshTime(String time) {
            mHeaderTimeView.Text = time;
        }

        private void InvokeOnScrolling() {
            /*if (mScrollListener instanceof OnXScrollListener) {
                OnXScrollListener l = (OnXScrollListener) mScrollListener;
                l.onXScrolling(this);
            }*/

            //Not really gonna work on C#, need to fix this.
            if (mScrollListener != null)
            {
                OnXScrollListener l = (OnXScrollListener)mScrollListener;
                l.OnXScrolling(this);
            }
        }

        private void UpdateHeaderHeight(float delta) {

            mHeaderView.SetVisibleHeight((int) delta + mHeaderView.GetVisibleHeight());

            if (mEnablePullRefresh && !mPullRefreshing) { // 未处于刷新状态，更新箭头

                if (mHeaderView.GetVisibleHeight() > mHeaderViewHeight) 
                {               
                    mHeaderView.SetState(XListViewHeader.STATE_READY);
                } 
                else 
                {
                    mHeaderView.SetState(XListViewHeader.STATE_NORMAL);
                }
            }
            SetSelection(0); // scroll to top each time ///Where is this?
        }

        /**
     * reset header view's height.
     */
        private void ResetHeaderHeight() {

            int height = mHeaderView.GetVisibleHeight();

            if (height == 0) // not visible.
                return;
            // refreshing and header isn't shown fully. do nothing.
            if (mPullRefreshing && height <= mHeaderViewHeight) 
                return;

            int finalHeight = 0; // default: scroll back to dismiss header.

            // is refreshing, just scroll back to show all the header.
            if (mPullRefreshing && height > mHeaderViewHeight) {
                finalHeight = mHeaderViewHeight;
            }

            mScrollBack = SCROLLBACK_HEADER;
            mScroller.StartScroll(0, height, 0, finalHeight - height,
                SCROLL_DURATION);
            // trigger computeScroll
            Invalidate();
        }

        private void UpdateFooterHeight(float delta) {
            int height = mFooterView.GetBottomMargin() + (int) delta;
            if (mEnablePullLoad && !mPullLoading) {
                if (height > PULL_LOAD_MORE_DELTA) { // height enough to invoke load
                    // more.
                    mFooterView.SetState(XListViewFooter.STATE_READY);
                } else {
                    mFooterView.SetState(XListViewFooter.STATE_NORMAL);
                }
            }
            mFooterView.SetBottomMargin(height);

            //      setSelection(mTotalItemCount - 1); // scroll to bottom
        }

        private void ResetFooterHeight() {
            int bottomMargin = mFooterView.GetBottomMargin();
            if (bottomMargin > 0) {
                mScrollBack = SCROLLBACK_FOOTER;
                mScroller.StartScroll(0, bottomMargin, 0, -bottomMargin,
                    SCROLL_DURATION);
                Invalidate();
            }
        }

        private void StartLoadMore() {
            mPullLoading = true;
            mFooterView.SetState(XListViewFooter.STATE_LOADING);
            if (mListViewListener != null) {
                mListViewListener.OnLoadMore();//nLoadMore();
            }
        }

        public override bool OnTouchEvent(MotionEvent ev) {
            if (mLastY == -1) {
                mLastY = ev.RawY;
            }

            switch (ev.Action) {
                case MotionEventActions.Down:
                    mLastY = ev.RawY;
                    break;
                case MotionEventActions.Move:
                    float deltaY = ev.RawY - mLastY;
                    mLastY = ev.RawY;
                    if (this.FirstVisiblePosition == 0
                        && (mHeaderView.GetVisibleHeight() > 0 || deltaY > 0)) {
                        // the first item is showing, header has shown or pull down.
                        UpdateHeaderHeight(deltaY / OFFSET_RADIO);
                        InvokeOnScrolling();
                    } else if (this.LastVisiblePosition == mTotalItemCount - 1
                        && (mFooterView.GetBottomMargin() > 0 || deltaY < 0)) {
                        // last item, already pulled up or want to pull up.
                        UpdateFooterHeight(-deltaY / OFFSET_RADIO);
                    }
                    break;
                default:
                    mLastY = -1; // reset
                    if (this.FirstVisiblePosition == 0)
                    {
                        // invoke refresh
                        if (mEnablePullRefresh
                            && mHeaderView.GetVisibleHeight() > mHeaderViewHeight) {
                            mPullRefreshing = true;
                            mHeaderView.SetState(XListViewHeader.STATE_REFRESHING);
                            if (mListViewListener != null) {
                                mListViewListener.OnRefresh();
                            }
                        }
                        ResetHeaderHeight();
                    } else if (this.LastVisiblePosition == mTotalItemCount - 1) {
                        // invoke load more.
                        if (mEnablePullLoad
                            && mFooterView.GetBottomMargin() > PULL_LOAD_MORE_DELTA
                            && !mPullLoading) {
                            StartLoadMore();
                        }
                        ResetFooterHeight();
                    }
                    break;
            }
            return base.OnTouchEvent(ev);//super.onTouchEvent(ev);
        }
            
        public override void ComputeScroll() {
            if (mScroller.ComputeScrollOffset()) {
                if (mScrollBack == SCROLLBACK_HEADER) {
                    mHeaderView.SetVisibleHeight(mScroller.CurrY);
                } else {
                    mFooterView.SetBottomMargin(mScroller.CurrY);
                }
                PostInvalidate();
                InvokeOnScrolling();
            }
            base.ComputeScroll();
        }
            
        public override void SetOnScrollListener(IOnScrollListener l) {
            mScrollListener = l;
        }
                       
        public void OnScroll(AbsListView view, int firstVisibleItem,
            int visibleItemCount, int totalItemCount) {
            // send to user's listener
            mTotalItemCount = totalItemCount;
            if (mScrollListener != null) {
                mScrollListener.OnScroll(view, firstVisibleItem, visibleItemCount,
                    totalItemCount);
            }
        }

        public void OnScrollStateChanged(AbsListView view, ScrollState scrollState)
        {
            if (mScrollListener != null)
            {
                mScrollListener.OnScrollStateChanged(view, scrollState);
            }
        }

        public void SetXListViewListener(IXListViewListener l) {
            mListViewListener = l;
        }

        /**
     * you can listen ListView.OnScrollListener or this one. it will invoke
     * onXScrolling when header/footer scroll back.
     */
        public interface OnXScrollListener : IOnScrollListener {
            void OnXScrolling(View view);
        }

        /**
     * implements this interface to get refresh/load more event.
     */
        public interface IXListViewListener {
            void OnRefresh();

            void OnLoadMore();
        }

        public class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
        {
            private readonly Action action;

            public GlobalLayoutListener(Action _action)
            {
                action = _action;
            }

            public void OnGlobalLayout()
            {
                if(action != null)
                    action.Invoke();
            }
        }

        public class OnClickListener : Java.Lang.Object, IOnClickListener
        {
            private readonly Action action;

            public OnClickListener(Action _action)
            {
                action = _action;
            }

            public void OnClick(View v)
            {
                if (action != null)
                    action.Invoke();
            }
        }
    }
}

