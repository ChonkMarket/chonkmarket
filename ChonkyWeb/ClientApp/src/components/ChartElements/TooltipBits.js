import React from 'react'

const TooltipBits = React.forwardRef(({series}, ref) => (
    <div>
        <span style={{color: `${series.type === 'area'? series.colorFull : series.color}`}}>{series.name} @ </span>
        <span ref={ref}></span>
    </div>
    )
)

export default TooltipBits
