namespace OxBox.Application.Abstractions;

public interface IBoxGatewayFactory
{
    IBoxGateway Create(string host, int port);
}
