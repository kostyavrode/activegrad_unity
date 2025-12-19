#import <UIKit/UIKit.h>

extern "C" {
    void _CopyToClipboard(const char* text) {
        if (text == NULL) {
            return;
        }
        
        NSString* nsString = [NSString stringWithUTF8String:text];
        if (nsString == nil) {
            return;
        }
        
        UIPasteboard* pasteboard = [UIPasteboard generalPasteboard];
        pasteboard.string = nsString;
    }
}

