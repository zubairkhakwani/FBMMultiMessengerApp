package FBM_Multi_Messenger.FBM_Multi_Messenger;

import android.util.Log;
import androidx.annotation.Keep;
import androidx.core.app.NotificationCompat;
import com.onesignal.notifications.IDisplayableMutableNotification;
import com.onesignal.notifications.INotificationReceivedEvent;
import com.onesignal.notifications.INotificationServiceExtension;
import org.json.JSONObject;

@Keep
public class NotificationServiceExtension implements INotificationServiceExtension {
    
    private static final String TAG = "NotificationExtension";
    
    @Override
    public void onNotificationReceived(INotificationReceivedEvent event) {
        IDisplayableMutableNotification notification = event.getNotification();
        
        try {
            // Get data from notification
            JSONObject data = notification.getAdditionalData();
            String chatId = data.optString("chatId", "");
            String message = data.optString("message", "");
            
            // Get sender name from heading
            String senderName = notification.getTitle();
            
            // Split messages by newline
            String[] messages = message.split("\n");
            
            // Get last message for collapsed view
            String lastMessage = messages.length > 0 ? messages[messages.length - 1] : message;
            
            // Build elegant notification with InboxStyle
            notification.setExtender(builder -> {
                
                // Create InboxStyle for multiple messages
                NotificationCompat.InboxStyle inboxStyle = new NotificationCompat.InboxStyle()
                    .setBigContentTitle(senderName);
                
                // Add summary if more than one message
                if (messages.length > 1) {
                    inboxStyle.setSummaryText(messages.length + " messages");
                }
                
                // Add each message as a separate line
                for (String msg : messages) {
                    if (msg != null && !msg.trim().isEmpty()) {
                        inboxStyle.addLine(msg.trim());
                    }
                }
                
                builder
                    .setContentTitle(senderName)
                    .setContentText(lastMessage)  // Show only last message in collapsed view
                    .setStyle(inboxStyle)         // Show all messages in expanded view
                    .setNumber(messages.length)   // Badge count
                    .setGroup("chat_" + chatId)
                    .setAutoCancel(true)
                    .setPriority(NotificationCompat.PRIORITY_HIGH);
                
                return builder;
            });
            
            // Display the notification
            notification.display();
            
        } catch (Exception e) {
            Log.e(TAG, "Error in notification extension", e);
            // Fallback: display original notification
            notification.display();
        }
    }
}