
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
    public class XListViewHeader : LinearLayout {

        private LinearLayout mContainer;
        private ImageView mArrowImageView;
        private ProgressBar mProgressBar;
        private TextView mHintTextView;
        private int mState = STATE_NORMAL;

        private Animation mRotateUpAnim;
        private Animation mRotateDownAnim;

        private const int ROTATE_ANIM_DURATION = 180;

        public const int STATE_NORMAL = 0;
        public const int STATE_READY = 1;
        public const int STATE_REFRESHING = 2;

        public XListViewHeader(Context context)
            : base(context)
        {
            Initialize();
            initView(context);
        }

        public XListViewHeader(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            Initialize();
            initView(context);
        }

        public XListViewHeader(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize();
            initView(context);
        }

        void Initialize()
        {
        }

        private void initView(Context context) {
            // 初始情况，设置下拉刷新view高度为0
            LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(
                LayoutParams.FillParent, 0);
            mContainer = (LinearLayout)LayoutInflater.From(context).Inflate(Resources.GetLayout(this), null);
               // Resources..layout.xlistview_header, null);
            mContainer.AddView(lp);
            this.SetGravity(GravityFlags.Bottom);

            mArrowImageView = FindViewById<ImageView>(Resource.Id.xlistview_header_arrow);
            mHintTextView = FindViewById<ImageView>(Resource.Id.xlistview_header_arrow);

            mRotateUpAnim = new RotateAnimation(0.0f, -180.0f, 1, 0.5f, 1, //1 == Animation.RELATIVE_TO_SELF in android. Wasnt sure where to find the cosntant.
                0.5f);
            mRotateUpAnim.Duration = ROTATE_ANIM_DURATION;
            mRotateUpAnim.FillAfter = true;
            mRotateDownAnim = new RotateAnimation(-180.0f, 0.0f,
                1, 0.5f, 1, 0.5f);
            mRotateDownAnim.Duration = ROTATE_ANIM_DURATION;
            mRotateDownAnim.FillAfter = true;
        }

        public void setState(int state) {
            if (state == mState) return ;

            if (state == STATE_REFRESHING) {    // 显示进度
                mArrowImageView.clearAnimation();
                mArrowImageView.setVisibility(View.INVISIBLE);
                mProgressBar.setVisibility(View.VISIBLE);
            } else {    // 显示箭头图片
                mArrowImageView.setVisibility(View.VISIBLE);
                mProgressBar.setVisibility(View.INVISIBLE);
            }

            switch(state){
                case STATE_NORMAL:
                    if (mState == STATE_READY) {
                        mArrowImageView.startAnimation(mRotateDownAnim);
                    }
                    if (mState == STATE_REFRESHING) {
                        mArrowImageView.clearAnimation();
                    }
                    mHintTextView.Text = Resource.String.xlistview_header_hint_normal;
                    break;
                case STATE_READY:
                    if (mState != STATE_READY) {
                        mArrowImageView.ClearAnimation();
                        mArrowImageView.StartAnimation(mRotateUpAnim);
                        mHintTextView.Text = Resource.String.xlistview_header_hint_ready;
                    }
                    break;
                case STATE_REFRESHING:
                    mHintTextView.Text = Resource.String.xlistview_header_hint_loading;
                    break;
                default:
            }

            mState = state;
        }

        public void setVisiableHeight(int height) {
            if (height < 0)
                height = 0;
            LinearLayout.LayoutParams lp = (LinearLayout.LayoutParams)mContainer.LayoutParameters;
            lp.Height = height;
            mContainer.LayoutParameters = lp;
        }

        public int getVisiableHeight() {
            return mContainer.Height;
        }
    }
}

