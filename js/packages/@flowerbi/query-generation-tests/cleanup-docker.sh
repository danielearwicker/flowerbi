#!/bin/bash

echo "Cleaning up Docker containers for FlowerBI tests..."

# Stop and remove all FlowerBI test containers
echo "Stopping FlowerBI test containers..."
docker stop $(docker ps -q -f name=FlowerBITest) 2>/dev/null || echo "No running FlowerBI test containers found"

echo "Removing FlowerBI test containers..."
docker rm -f $(docker ps -aq -f name=FlowerBITest) 2>/dev/null || echo "No FlowerBI test containers found"

# Also clean up any containers with FlowerBI in the name
echo "Removing any other FlowerBI containers..."
docker rm -f $(docker ps -aq -f name=FlowerBI) 2>/dev/null || echo "No other FlowerBI containers found"

echo "Docker cleanup complete!"
echo "You can now run 'npm test' safely."