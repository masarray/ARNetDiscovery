# GitHub Pages Deployment

The product page is deployed with GitHub Actions from the `main` branch. The public URL is:

```text
https://masarray.github.io/ARNetDiscovery/
```

The workflow uploads the `docs` folder as the Pages artifact root, so `docs/index.html` becomes the public site root. The repository does not need a `gh-pages` branch or branch-based Pages publishing.

## Required repository setting

In GitHub, open:

```text
Settings -> Pages -> Build and deployment -> Source -> GitHub Actions
```

After that, every push to `main` that changes `docs/**` or the Pages workflow can deploy the product page.
