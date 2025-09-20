function feature {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $Name = $Name.ToLowerInvariant().Replace(' ', '-')
    $BranchName = $Name.StartsWith('feature/') ? $Name : "feature/$Name"
    git switch -c $BranchName
}