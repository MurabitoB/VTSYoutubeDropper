import path from 'path';
import { MockServer } from 'mqk-mock-server';

const config = require('./mock-home/msconfig.json');

const mockServer = new MockServer({
  mockHome: path.resolve(__dirname, './mock-home')
});

const server = mockServer.listen(config.port, () => {
  console.log(`Mock Server is running on port: ${config.port}`);
});
