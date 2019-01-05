import { Component, OnInit, Input } from '@angular/core';
import { Message } from '../../models/message';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';
import { AlertifyService } from '../../services/alertify.service';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-member-messages',
  templateUrl: './member-messages.component.html',
  styleUrls: ['./member-messages.component.css']
})
export class MemberMessagesComponent implements OnInit {
  @Input() recipientId: number;
  messages: Message[];
  newMessage: any = {};

  constructor(private userService: UserService, private authService: AuthService, private alertifyService: AlertifyService) { }

  ngOnInit() {
    this.loadMessages();
  }

  loadMessages() {
    const currentUserId = +this.authService.decodedToken.nameid;
    this.userService.getMessageThread(currentUserId, this.recipientId).pipe(
      tap(messages => {
        messages.forEach(message => {
          if (!message.isRead && message.recipientId === currentUserId) {
            this.userService.markAsRead(message.id, currentUserId);
          }
        })
      })
    ).subscribe(messages => {
      this.messages = messages;
    }, error => {
      this.alertifyService.error(error);
    });
  }

  sendMessage() {
    this.newMessage.recipientId = this.recipientId;
    this.userService.sendMessage(this.authService.decodedToken.nameid, this.newMessage).subscribe((message: Message) => {
      this.messages.unshift(message);
      this.newMessage.content = '';
    }, error => {
      this.alertifyService.error(error);
    });
  }
}
