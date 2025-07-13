const path = require('path');

module.exports = {
  entry: './src/bundle.ts',
  target: 'web',
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: [
          {
            loader: 'babel-loader',
            options: {
              presets: [
                ['@babel/preset-env', {
                  targets: { ie: '11' },
                  useBuiltIns: false
                }]
              ]
            }
          },
          'ts-loader'
        ],
        exclude: /node_modules/,
      },
    ],
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
  },
  output: {
    filename: 'flowerbi-query-generation.js',
    path: path.resolve(__dirname, 'bundle'),
    library: {
      name: 'FlowerBIModule',
      type: 'umd',
    },
    globalObject: 'this',
  },
  optimization: {
    minimize: true,
  },
  externals: {
    // No externals - we want everything bundled
  },
  node: {
    // Disable node.js specific features for better compatibility
    __dirname: false,
    __filename: false,
  }
};