﻿
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
using Android.Animation;

using XListViewSharp;

namespace XListViewSharp
{
    //Most of this is pulled straight from the java src, since the syntax is so similar.
    public class XListView : ListView, ListView.IOnScrollListener
    {

        private float mLastY = -1; // save event y
        private Scroller mScroller; // used for scroll back
        //private OnScrollListener mScrollListener; // user's scroll listener

        // the interface to trigger refresh and load more.
        //private IXListViewListener mListViewListener;

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
            mScroller = new Scroller(context, Android.Views.Animations.DecelerateInterpolator);
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

            // init header heigh
            mHeaderView.ViewTreeObserver.AddOnGlobalLayoutListener( () => {
                mHeaderViewHeight = mHeaderViewContent.Height;
                mHeaderView.ViewTreeObserver.RemoveGlobalOnLayoutListener(this); //?
            }
            );
                
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
            
        public override void SetAdapter (Android.Widget.IListAdapter adapter) {
            // make sure XListViewFooter is the last footer view, and only add once.
            if (mIsFooterReady == false) {
                mIsFooterReady = true;
                AddFooterView(mFooterView);
            }
            base.SetAdapter(adapter);
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
                mFooterView.show();
                mFooterView.SetState(XListViewFooter.STATE_NORMAL);
                // both "pull up" and "click" will invoke load more.

                mFooterView.OnClick += () =>
                {
                    mFooterView.StartLoadMore(); //Have to implement these in Footer.
                };

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
        public void stopRefresh() {
            if (mPullRefreshing == true) {
                mPullRefreshing = false;
                ResetHeaderHeight();
            }
        }

        /**
     * stop load more, reset footer view.
     */
        public void stopLoadMore() {
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
        public void setRefreshTime(String time) {
            mHeaderTimeView.Text = time;
        }

        private void invokeOnScrolling() {
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

                if (mHeaderView.getVisiableHeight() > mHeaderViewHeight) 
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

            int height = mHeaderView.GetVisiableHeight();

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
            int height = mFooterView.getBottomMargin() + (int) delta;
            if (mEnablePullLoad && !mPullLoading) {
                if (height > PULL_LOAD_MORE_DELTA) { // height enough to invoke load
                    // more.
                    mFooterView.setState(XListViewFooter.STATE_READY);
                } else {
                    mFooterView.setState(XListViewFooter.STATE_NORMAL);
                }
            }
            mFooterView.setBottomMargin(height);

            //      setSelection(mTotalItemCount - 1); // scroll to bottom
        }

        private void resetFooterHeight() {
            int bottomMargin = mFooterView.getBottomMargin();
            if (bottomMargin > 0) {
                mScrollBack = SCROLLBACK_FOOTER;
                mScroller.startScroll(0, bottomMargin, 0, -bottomMargin,
                    SCROLL_DURATION);
                invalidate();
            }
        }

        private void startLoadMore() {
            mPullLoading = true;
            mFooterView.setState(XListViewFooter.STATE_LOADING);
            if (mListViewListener != null) {
                mListViewListener.onLoadMore();
            }
        }

        public override bool onTouchEvent(MotionEvent ev) {
            if (mLastY == -1) {
                mLastY = ev.getRawY();
            }

            switch (ev.getAction()) {
                case MotionEvent.ACTION_DOWN:
                    mLastY = ev.getRawY();
                    break;
                case MotionEvent.ACTION_MOVE:
                    final float deltaY = ev.getRawY() - mLastY;
                    mLastY = ev.getRawY();
                    if (getFirstVisiblePosition() == 0
                        && (mHeaderView.getVisiableHeight() > 0 || deltaY > 0)) {
                        // the first item is showing, header has shown or pull down.
                        updateHeaderHeight(deltaY / OFFSET_RADIO);
                        invokeOnScrolling();
                    } else if (getLastVisiblePosition() == mTotalItemCount - 1
                        && (mFooterView.getBottomMargin() > 0 || deltaY < 0)) {
                        // last item, already pulled up or want to pull up.
                        updateFooterHeight(-deltaY / OFFSET_RADIO);
                    }
                    break;
                default:
                    mLastY = -1; // reset
                    if (getFirstVisiblePosition() == 0) {
                        // invoke refresh
                        if (mEnablePullRefresh
                            && mHeaderView.getVisiableHeight() > mHeaderViewHeight) {
                            mPullRefreshing = true;
                            mHeaderView.setState(XListViewHeader.STATE_REFRESHING);
                            if (mListViewListener != null) {
                                mListViewListener.onRefresh();
                            }
                        }
                        resetHeaderHeight();
                    } else if (getLastVisiblePosition() == mTotalItemCount - 1) {
                        // invoke load more.
                        if (mEnablePullLoad
                            && mFooterView.getBottomMargin() > PULL_LOAD_MORE_DELTA
                            && !mPullLoading) {
                            startLoadMore();
                        }
                        resetFooterHeight();
                    }
                    break;
            }
            return super.onTouchEvent(ev);
        }
            
        public override void computeScroll() {
            if (mScroller.computeScrollOffset()) {
                if (mScrollBack == SCROLLBACK_HEADER) {
                    mHeaderView.setVisiableHeight(mScroller.getCurrY());
                } else {
                    mFooterView.setBottomMargin(mScroller.getCurrY());
                }
                postInvalidate();
                invokeOnScrolling();
            }
            super.computeScroll();
        }
            
        public override void setOnScrollListener(OnScrollListener l) {
            mScrollListener = l;
        }
            
        public override void onScrollStateChanged(AbsListView view, int scrollState) {
            if (mScrollListener != null) {
                mScrollListener.onScrollStateChanged(view, scrollState);
            }
        }
            
        public override void onScroll(AbsListView view, int firstVisibleItem,
            int visibleItemCount, int totalItemCount) {
            // send to user's listener
            mTotalItemCount = totalItemCount;
            if (mScrollListener != null) {
                mScrollListener.onScroll(view, firstVisibleItem, visibleItemCount,
                    totalItemCount);
            }
        }

        public override void setXListViewListener(IXListViewListener l) {
            mListViewListener = l;
        }

        /**
     * you can listen ListView.OnScrollListener or this one. it will invoke
     * onXScrolling when header/footer scroll back.
     */
        public interface OnXScrollListener extends OnScrollListener {
            public void onXScrolling(View view);
        }

        /**
     * implements this interface to get refresh/load more event.
     */
        public interface IXListViewListener {
            public void onRefresh();

            public void onLoadMore();
        }
    }
}

