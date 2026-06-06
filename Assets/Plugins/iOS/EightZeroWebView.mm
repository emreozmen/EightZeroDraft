#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>

static WKWebView *EightZeroWebView = nil;

static UIViewController *EightZeroRootViewController(void) {
    UIWindow *window = UIApplication.sharedApplication.keyWindow;
    if (window == nil) {
        window = UIApplication.sharedApplication.windows.firstObject;
    }
    return window.rootViewController;
}

extern "C" void EightZero_ShowWebView(const char *urlCString) {
    dispatch_async(dispatch_get_main_queue(), ^{
        UIViewController *root = EightZeroRootViewController();
        if (root == nil || urlCString == nil) {
            return;
        }

        NSString *urlString = [NSString stringWithUTF8String:urlCString];
        NSURL *fileURL = [NSURL fileURLWithPath:urlString];
        NSURL *readAccessURL = [fileURL URLByDeletingLastPathComponent];

        if (EightZeroWebView == nil) {
            WKWebViewConfiguration *configuration = [[WKWebViewConfiguration alloc] init];
            configuration.allowsInlineMediaPlayback = YES;
            configuration.preferences.javaScriptCanOpenWindowsAutomatically = YES;

            EightZeroWebView = [[WKWebView alloc] initWithFrame:root.view.bounds configuration:configuration];
            EightZeroWebView.autoresizingMask = UIViewAutoresizingFlexibleWidth | UIViewAutoresizingFlexibleHeight;
            EightZeroWebView.backgroundColor = UIColor.clearColor;
            EightZeroWebView.opaque = NO;
            EightZeroWebView.scrollView.bounces = NO;
            EightZeroWebView.scrollView.contentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentNever;

            [root.view addSubview:EightZeroWebView];
        }

        [EightZeroWebView loadFileURL:fileURL allowingReadAccessToURL:readAccessURL];
    });
}

extern "C" void EightZero_HideWebView(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (EightZeroWebView != nil) {
            [EightZeroWebView removeFromSuperview];
            EightZeroWebView = nil;
        }
    });
}
