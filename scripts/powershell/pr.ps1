function pr {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$Title,

        [Parameter(Mandatory = $false)]
        [string]$TargetBranch = "main"
    )

    gh pr create -b "" -a "@me" -t "$Title" --base "$TargetBranch"
}