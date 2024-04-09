package config

import (
	"fmt"

	"github.com/spf13/viper"
)

type (
	// Config -.
	Config struct {
		HTTP       `yaml:"http"`
		Log        `yaml:"log"`
		Pubsub     `yaml:"pubsub"`
		Statestore `yaml:"statestore"`
		Dapr       `yaml:"dapr"`
	}

	// HTTP -.
	HTTP struct {
		Port string `yaml:"port"`
	}

	// Log -.
	Log struct {
		Level string `yaml:"level"`
	}

	Pubsub struct {
		Name  string `yaml:"name"`
		Topic string `yaml:"topic"`
	}

	Statestore struct {
		Name string `yaml:"name"`
	}

	Dapr struct {
		Port string `yaml:"port"`
	}
)

// LoadConfig reads configuration from file or environment variables.
func NewConfig(path string) (*Config, error) {
	viper.AddConfigPath(path)
	viper.SetConfigName("config")
	viper.SetConfigType("yaml")
	viper.SetEnvPrefix("APP")

	viper.AutomaticEnv()

	if err := viper.ReadInConfig(); err != nil {
		return nil, fmt.Errorf("error reading config: %w", err)
	}

	var config Config
	if err := viper.Unmarshal(&config); err != nil {
		return nil, fmt.Errorf("error unmarshaling config: %w", err)

	}

	return &config, nil
}
