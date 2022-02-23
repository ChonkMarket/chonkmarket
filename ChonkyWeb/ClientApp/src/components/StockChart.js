import * as d3 from 'd3';
import React, { useState } from 'react';
import { useD3 } from './hooks/useD3';

function StockChart({ data, containerWidth, containerHeight }) {
  const [tooltip, setTooltip] = useState();
  const svgMargin = { top: 0, right: 20, bottom: 20, left: 20 };
  const margin = { top: 25, right: 35, bottom: 20, left: 35 };
  const width = containerWidth - margin["left"] - margin["right"];
  const height = containerHeight - margin["top"] - margin["bottom"];
  const symbol = data.length > 0 ? data[0].symbol : "";
  const description = data.length > 0 ? data[0].description : "";
  let price = (Math.round(data[data.length - 1].mark * 100) / 100).toFixed(2);
  let nope = (Math.round(data[data.length - 1].nope * 100) / 100).toFixed(2);

  const xMin = d3.min(data, d => { return d['quoteTime'] });
  const xMax = d3.max(data, d => { return d['quoteTime'] });
  const xScale = d3
    .scaleTime()
    .domain([xMin, xMax])
    .range([margin["left"], width - margin["right"]]);

  const yMin = d3.min(data, d => { return d['mark'] });
  const yMax = d3.max(data, d => { return d['mark'] });
  const yScale = d3
    .scaleLinear()
    .domain([yMax, yMin])
    .range([margin["bottom"], height - margin["top"]]);

  const nopeMin = d3.min(data, d => { return d['nope'] });
  const nopeMax = d3.max(data, d => { return d['nope'] });
  const nopeScale = d3
    .scaleLinear()
    .domain([nopeMax, nopeMin])
    .range([margin["bottom"], height - margin["top"]]);

  const ref = useD3(
    (svg) => {
      if (data.length === 0) {
        return (<svg></svg>);
      }
      svg.selectAll("*").remove();

      svg.append('g')
        .attr('id', 'xAxis')
        .style('font-size', '16px')
        .attr('transform', `translate(0, ${height - margin["bottom"]})`)
        .call(d3.axisBottom(xScale).tickFormat(d3.timeFormat("%I:%M %p")));

      svg.append('g')
        .attr('id', 'yAxis')
        .style('font-size', '16px')
        .attr('transform', `translate(${width - margin["right"]}, 0)`)
        .call(d3.axisRight(yScale).tickFormat(d3.format('.2f')));

      svg.append('g')
        .attr('id', 'nopeAxis')
        .style('font-size', '16px')
        .attr('transform', `translate(${margin["left"]}, 0)`)
        .call(d3.axisLeft(nopeScale));

      const line = d3
        .line()
        .x(d => {
          return xScale(d['quoteTime']);
        })
        .y(d => {
          return yScale(d['mark']);
        });

      const line2 = d3
        .line()
        .x(d => {
          return xScale(d['quoteTime']);
        })
        .y(d => {
          return nopeScale(d['nope']);
        });

      svg.append('path')
        .data([data])
        .style('fill', 'none')
        .attr('id', 'priceChart')
        .attr('stroke', 'steelblue')
        .attr('stroke-width', '1.5')
        .attr('d', line);

      svg.append('path')
        .data([data])
        .style('fill', 'none')
        .attr('id', 'nopeChart')
        .attr('stroke', 'crimson')
        .attr('stroke-width', '1.5')
        .attr('d', line2);

      const yLegendCoord = { horizontal: width / 24, vertical: height / 16 }

      svg.append("circle")
        .attr("cx", yLegendCoord.horizontal)
        .attr("cy", yLegendCoord.vertical)
        .attr("r", 5)
        .style("fill", "steelblue")

      svg.append("circle")
        .attr("cx", yLegendCoord.horizontal)
        .attr("cy", yLegendCoord.vertical + 30)
        .attr("r", 5)
        .style("fill", "crimson")

      svg.append("text")
        .attr("x", yLegendCoord.horizontal + 15)
        .attr("y", yLegendCoord.vertical)
        .text(`Stock Price : $${price}`)
        .style("fill", "#67809f")
        .style("font-size", "18px")
        .attr("alignment-baseline", "middle")

      svg.append("text")
        .attr("x", yLegendCoord.horizontal + 15)
        .attr("y", yLegendCoord.vertical + 30)
        .text(`Nope : ${nope}`)
        .style("fill", "crimson")
        .style("font-size", "18px")
        .attr("alignment-baseline", "middle")

      svg.append("text")
        .attr("x", width / 2)
        .attr("y", 20)
        .style("text-anchor", "middle")
        .style("fill", "white")
        .style('font-size', '18px')
        .text(`${symbol} - ${description}`);
    },
    [data.length, containerWidth, containerHeight, tooltip]
  );

  function findClosestQuote(data, quoteTime, min, mid, max) {
    if (mid == min) {
      return data[min];
    } else if (mid == max) {
      return data[max];
    }
    if (data[mid].quoteTime === quoteTime) {
      return data[mid].quoteTime;
    } else if (data[mid].quoteTime > quoteTime) {
      return findClosestQuote(data, quoteTime, min, Math.floor((mid - min) / 2), mid);
    }
    return findClosestQuote(data, quoteTime, mid, Math.floor((max - mid) / 2) + mid, max);
  }

  function mouseHover(e) {
    var xPos = e.clientX - svgMargin["left"];
    var hoverTime = xScale.invert(xPos).getTime();
    var closestQuote = findClosestQuote(data, hoverTime, 0, Math.floor(data.length / 2), data.length);
    if (closestQuote.nope) {
      var tooltipText = ` ${new Date(closestQuote.quoteTime).toLocaleTimeString()} - Nope @ ${closestQuote.nope.toFixed(2)} - Price @ $${closestQuote.mark}`
      setTooltip(<span>{tooltipText}</span>)
    }
  }

  function debounce(fn, ms) {
    let timer;
    return (e) => {
      e.persist();
      clearTimeout(timer)
      timer = setTimeout(_ => {
        timer = null;
        fn.call(this, e)
      }, ms)
    };
  }

  let throttledMouseOver = debounce(mouseHover, 25);

  return (
    <div>
      <div className={'info-header'} style={{ "textAlign": "center", "color":"white" }}>
        <span>{symbol} {description}</span>
        {tooltip}
      </div>
      <svg
        onMouseMove={ throttledMouseOver  }
        ref={ref}
        style={{
          height: containerHeight - 45,
          width: containerWidth - 40,
          marginTop: svgMargin["top"],
          marginBottom: svgMargin["bottom"],
          marginLeft: svgMargin["left"],
          marginRight: svgMargin["right"]
        }}
      >
        <g style={{ transform: 'translate(20, 20)' }} />
      </svg>
    </div>
  );
}

export default StockChart;
