export default [
  {
    path: ["/watch"],
    methods: ["GET"],
    controller: "watch",
  },
  {
    path: ["/live_chat"],
    methods: ["GET"],
    controller: "live_chat_dom",
  },
  {
    path: ["/youtubei/v1/live_chat/get_live_chat"],
    methods: ["GET", "POST"],
    controller: "live_chat",
  },
];
