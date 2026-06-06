#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>

static WKWebView *EightZeroWebView = nil;

static void EightZero_LoadUrlString(NSString *urlString, NSInteger attempt);

static UIViewController *EightZeroRootViewController(void) {
    UIWindow *window = UIApplication.sharedApplication.keyWindow;

    if (window == nil) {
        if (@available(iOS 13.0, *)) {
            for (UIScene *scene in UIApplication.sharedApplication.connectedScenes) {
                if (scene.activationState != UISceneActivationStateForegroundActive || ![scene isKindOfClass:UIWindowScene.class]) {
                    continue;
                }

                UIWindowScene *windowScene = (UIWindowScene *)scene;
                for (UIWindow *candidate in windowScene.windows) {
                    if (candidate.isKeyWindow) {
                        window = candidate;
                        break;
                    }
                }

                if (window != nil) {
                    break;
                }
            }
        }
    }

    if (window == nil) {
        window = UIApplication.sharedApplication.windows.firstObject;
    }

    return window.rootViewController;
}

extern "C" void EightZero_ShowWebView(const char *urlCString) {
    if (urlCString == nil) {
        return;
    }

    NSString *urlString = [NSString stringWithUTF8String:urlCString];
    if (urlString == nil) {
        return;
    }

    dispatch_async(dispatch_get_main_queue(), ^{
        EightZero_LoadUrlString(urlString, 0);
    });
}

static void EightZero_LoadUrlString(NSString *urlString, NSInteger attempt) {
    UIViewController *root = EightZeroRootViewController();
    if (root == nil) {
        if (attempt < 20) {
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.25 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
                EightZero_LoadUrlString(urlString, attempt + 1);
            });
        }
        return;
    }

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
}

extern "C" void EightZero_HideWebView(void) {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (EightZeroWebView != nil) {
            [EightZeroWebView removeFromSuperview];
            EightZeroWebView = nil;
        }
    });
}
