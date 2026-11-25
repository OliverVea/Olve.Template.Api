// The service's own service-worker handlers. The build appends this file to shell.js in dist/sw.js,
// so add listeners here instead of editing the shell. The shell answers page loads and its precached
// files; a "fetch" listener here must leave those alone (a second respondWith throws). For example:
//
// self.addEventListener("push", (event) => {
//   const message = event.data?.json() ?? {};
//   event.waitUntil(self.registration.showNotification(message.title ?? "Olve.Template.Api", message));
// });
//
// self.addEventListener("notificationclick", (event) => {
//   event.notification.close();
//   event.waitUntil(self.clients.openWindow(event.notification.data?.url ?? "/"));
// });
