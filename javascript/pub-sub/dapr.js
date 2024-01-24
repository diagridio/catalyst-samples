import { DaprClient, CommunicationProtocolEnum } from "@dapr/dapr";

const daprApiToken = process.env.DAPR_API_TOKEN;

export const client = new DaprClient({
  daprApiToken: daprApiToken,
  communicationProtocol: CommunicationProtocolEnum.HTTP
});

